using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyKnob.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyKnob.Core
{
    class SpotifyConnection
    {
        private const int _stateGetterCooldown = 2000;
        private const int _stateRefreshCooldown = 30000;

        public event Action<bool, string> ConnectionReceivedEvent;
        public event Action<bool, string> ProfileReceivedEvent;
        public event Action<bool, string> CurrentStateReceivedEvent;
        public event Action StateRefreshed;

        public CurrentlyPlayingContext CurrentState { get; private set; }
        public PrivateUser Profile { get; private set; }

        private UserSaveData _userData;
        private EmbedIOAuthServer _server;
        private SpotifyClient _client;
        private AuthSaveData _authSaveData;

        private CancellationTokenSource _stateRefreshCTS;

        private bool _isStateGetterOnCooldown = false;

        public void SetUserData(UserSaveData userData)
        {
            _userData = userData;
        }

        public async void CreateSpotifyClient()
        {
            if(AuthSaveDataHelper.LoadAuthData(out _authSaveData))
            {
                CreateSpotifyClient(_authSaveData);

                return;
            }

            _server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, _userData.ClientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.UserReadPlaybackState, Scopes.UserModifyPlaybackState }
            };


            var (verifier, challenge) = PKCEUtil.GenerateCodes();
            _authSaveData.Verifier = verifier;
            var loginRequest = new LoginRequest(new Uri("http://localhost:5543/callback"), _userData.ClientId, LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = new[] { Scopes.UserReadEmail, Scopes.UserReadPlaybackState, Scopes.UserModifyPlaybackState }
            };

            BrowserUtil.Open(loginRequest.ToUri());
        }

        public async void GetUserProfile()
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                Profile = await _client.UserProfile.Current(default);

                ProfileReceivedEvent?.Invoke(true, null);
            }
            catch (Exception ex)
            {
                ProfileReceivedEvent?.Invoke(false, ex.Message);
            }
        }

        public async void GetCurrentState()
        {
            if (_client == null)
            {
                return;
            }

            if (_isStateGetterOnCooldown)
            {
                return;
            }

            _isStateGetterOnCooldown = true;

            try
            {
                var state = await _client.Player.GetCurrentPlayback(default);
                CurrentState = state;

                CurrentStateReceivedEvent?.Invoke(true, null);
            }
            catch (Exception ex)
            {
                CurrentStateReceivedEvent?.Invoke(false, ex.Message);
            }

            await Task.Delay(_stateGetterCooldown);

            _isStateGetterOnCooldown = false;
        }

        public async void ChangeVolume(int newVolume)
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                await _client.Player.SetVolume(new PlayerVolumeRequest(newVolume), default);
            }
            catch (Exception ex)
            {
                if (ex is APIException apiEx && apiEx.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    CurrentState = null;
                }
            }
        }

        public async void Pause()
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                await _client.Player.PausePlayback(default);
            }
            catch (Exception)
            {
            }
        }

        public async void Resume()
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                await _client.Player.ResumePlayback(default);
            }
            catch (Exception)
            {
            }
        }

        private async void CreateSpotifyClient(AuthSaveData authSaveData)
        {
            bool success = true;
            string error = null;

            try
            {
                var tokenResponse = await new OAuthClient().RequestToken(
                  new PKCETokenRefreshRequest(_userData.ClientId, authSaveData.RefreshToken)
                );

                var authenticator = new PKCEAuthenticator(_userData.ClientId, tokenResponse);
                _client = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
                _authSaveData.AuthToken = tokenResponse.AccessToken;
                _authSaveData.RefreshToken = tokenResponse.RefreshToken;

                AuthSaveDataHelper.SaveAuthData(_authSaveData);

                _stateRefreshCTS = new CancellationTokenSource();

                Task.Factory.StartNew(RefreshState);
            }
            catch (Exception ex)
            {
                success = false;
                error = ex.Message;
            }

            ConnectionReceivedEvent?.Invoke(success, error);
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            var tokenResponse = await new OAuthClient().RequestToken(
                new PKCETokenRequest(_userData.ClientId, response.Code, _server.BaseUri, _authSaveData.Verifier));
            var authenticator = new PKCEAuthenticator(_userData.ClientId, tokenResponse);
            _client = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator));
            _authSaveData.AuthToken = tokenResponse.AccessToken;
            _authSaveData.RefreshToken = tokenResponse.RefreshToken;

            AuthSaveDataHelper.SaveAuthData(_authSaveData);
            ConnectionReceivedEvent?.Invoke(true, null);
        }

        private async void RefreshState()
        {
            while(true)
            {
                if(_stateRefreshCTS.Token.IsCancellationRequested)
                {
                    break;
                }

                Thread.Sleep(_stateRefreshCooldown);

                try
                {
                    var state = await _client.Player.GetCurrentPlayback(default);
                    CurrentState = state;

                    StateRefreshed?.Invoke();
                }
                catch (Exception)
                {
                }
            }
        }

        private async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");

            await _server.Stop();

            ConnectionReceivedEvent?.Invoke(false, error);
        }
    }
}
