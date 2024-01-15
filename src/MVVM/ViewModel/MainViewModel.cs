using SpotifyKnob.Core;
using SpotifyKnob.MVVM.Model;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows;
using SnagFree.TrayApp.Core;
using System.Collections.Generic;

namespace SpotifyKnob.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        private const int _volumeCooldown = 500;

        public ObservableCollection<HotkeyModel> Hotkeys { get; set; }

        public RelayCommand ConnectCommand { get; set; }
        

        public ConnectionState ConnectionState
        {
            get { return _connectionState; }
            set { _connectionState = value; OnPropertyChanged(); }
        }

        public string ProfileName
        {
            get { return _profileName; }
            set { _profileName = value; OnPropertyChanged(); }
        }

        public string ProfileImage
        {
            get { return _profileImage; }
            set { _profileImage = value; OnPropertyChanged(); }
        }

        public int CurrentVolume
        {
            get { return _currentVolume; }
            set { _currentVolume = value; OnPropertyChanged(); }
        }

        public HotkeyModel SelectedHotkey 
        { 
            get
            {
                return _selectedHotkey;
            }
            set
            {
                _selectedHotkey = value;
                OnPropertyChanged();
            }
        }

        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private string _profileName = "None";
        private string _profileImage = "";
        private HotkeyModel _selectedHotkey = null;
        private UserSaveData _userSaveData;
        private HotkeysSaveData _hotkeysSaveData;
        private int _currentVolume = 0;
        private SpotifyConnection _spotify;
        private GlobalKeyboardHook _keyboardHook;
        private bool _isVolumeOnCooldown = false;
        private bool _isMuted => CurrentVolume == 0;
        private int _unmuteVolume = 0;
        private bool _isPaused = false;

        public MainViewModel()
        {
            _spotify = new SpotifyConnection();
            _keyboardHook = new GlobalKeyboardHook();
            ConnectCommand = new RelayCommand(Connect);
            
            _keyboardHook.KeyboardPressed += OnKeyboardPressed;

            InitializeHotkeys();
            InitializeHotkeysSaveData();
            LoadHotkeys();

            if (!UserSaveDataHelper.LoadUserData(out _userSaveData))
            {
                _userSaveData = new UserSaveData();
                UserSaveDataHelper.SaveUserData(_userSaveData);
            }

            _spotify.SetUserData(_userSaveData);
        }

        private void OnKeyboardPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            if(e.KeyboardState != GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                return;
            }

            Key key = KeyInterop.KeyFromVirtualKey(e.KeyboardData.VirtualCode);

            if (_selectedHotkey == null)
            {
                if(_spotify.CurrentState == null)
                {
                    _spotify.GetCurrentState();

                    return;
                }

                foreach (var hotkey in Hotkeys)
                {
                    if (hotkey.BoundKey != key)
                    {
                        continue;
                    }

                    if(hotkey.IsOnCooldown)
                    {
                        break;
                    }

                    if(hotkey.Cooldown > 0)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            hotkey.IsOnCooldown = true;
                            hotkey.BoundAction.Invoke();

                            await Task.Delay(hotkey.Cooldown);

                            hotkey.IsOnCooldown = false;
                        });
                    }
                    else
                    {
                        hotkey.BoundAction?.Invoke();
                    }

                    break;
                }

                return;
            }

            int selectedIndex = Hotkeys.IndexOf(_selectedHotkey);

            if (key == Key.Escape)
            {
                SetHotkey(selectedIndex, _selectedHotkey.BoundKey);

                return;
            }

            int conflictingIndex = GetConflictingHotkey(key);

            if (selectedIndex == conflictingIndex)
            {
                return;
            }

            if (conflictingIndex != -1)
            {
                SetHotkey(conflictingIndex, Key.None);
                UpdateHotkeySaveData(Hotkeys[conflictingIndex].HotkeyType, Key.None);
            }

            SetHotkey(selectedIndex, key);
            UpdateHotkeySaveData(Hotkeys[selectedIndex].HotkeyType, key);
            HotkeysSaveDataHelper.SaveHotkeys(_hotkeysSaveData);
        }

        private void SetHotkey(int index, Key binding)
        {
            var hotkey = new HotkeyModel(Hotkeys[index]);
            hotkey.BoundKey = binding;

            Hotkeys[index] = hotkey;
        }

        private void UpdateHotkeySaveData(HotkeyType hotkeyType, Key binding)
        {
            var hotkeySaveData = _hotkeysSaveData.Hotkeys.Find(saved => saved.Type == (uint)hotkeyType);

            hotkeySaveData.BoundKey = (uint)binding;
        }

        private void InitializeHotkeys()
        {
            Hotkeys = new ObservableCollection<HotkeyModel>();

            Hotkeys.Add(new HotkeyModel
            {
                DisplayName = "Volume Up Hotkey",
                HotkeyType = HotkeyType.VolumeUp,
                BoundAction = TurnVolumeUp
            });
            Hotkeys.Add(new HotkeyModel
            {
                DisplayName = "Volume Down Hotkey",
                HotkeyType = HotkeyType.VolumeDown,
                BoundAction = TurnVolumeDown
            });
            Hotkeys.Add(new HotkeyModel
            {
                DisplayName = "Mute/Unmute Hotkey",
                HotkeyType = HotkeyType.Mute,
                BoundAction = MuteUnmute,
                Cooldown = 125
            });
            Hotkeys.Add(new HotkeyModel
            {
                DisplayName = "Play/Pause Hotkey",
                HotkeyType = HotkeyType.Pause,
                BoundAction = PauseResume,
                Cooldown = 125
            });
        }

        private void InitializeHotkeysSaveData()
        {
            _hotkeysSaveData = new HotkeysSaveData();
            _hotkeysSaveData.Hotkeys = new List<HotkeySaveData>();

            foreach(var hotkey in Hotkeys)
            {
                var hotkeySaveData = new HotkeySaveData
                {
                    Type = (uint)hotkey.HotkeyType,
                    BoundKey = (uint)hotkey.BoundKey
                };

                _hotkeysSaveData.Hotkeys.Add(hotkeySaveData);
            }

            if (HotkeysSaveDataHelper.LoadHotkeys(out var hotkeysSaveData))
            {
                foreach(var hotkey in _hotkeysSaveData.Hotkeys)
                {
                    var savedHotkey = hotkeysSaveData.Hotkeys.Find(h => h.Type == hotkey.Type);

                    if(savedHotkey == null)
                    {
                        continue;
                    }

                    hotkey.BoundKey = savedHotkey.BoundKey;
                }
            }

            HotkeysSaveDataHelper.SaveHotkeys(_hotkeysSaveData);
        }

        private void LoadHotkeys()
        {
            for (int i = 0; i < Hotkeys.Count; i++)
            {
                HotkeySaveData saveData = _hotkeysSaveData.Hotkeys.Find(saved => saved.Type == (uint)Hotkeys[i].HotkeyType);

                if (saveData == null)
                {
                    continue;
                }

                SetHotkey(i, (Key)saveData.BoundKey);
            }
        }

        private int GetConflictingHotkey(Key key)
        {
            for(int i = 0; i < Hotkeys.Count; i++)
            {
                if(Hotkeys[i].BoundKey == key)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Connect(object o)
        {
            if(!ValidateUserData())
            {
                MessageBox.Show("Please fill out API data in the User.xml file!", "Spotify Knob", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            _spotify.ConnectionReceivedEvent += OnConnectionResponse;
            new Task(_spotify.CreateSpotifyClient).Start();

            ConnectionState = ConnectionState.Connecting;
        }

        private bool ValidateUserData()
        {
            return _userSaveData != null
                && !string.IsNullOrEmpty(_userSaveData.ClientId)
                && _userSaveData.ClientId != "CLIENT_ID"
                && !string.IsNullOrEmpty(_userSaveData.Username)
                && _userSaveData.Username != "USERNAME"
                && !string.IsNullOrEmpty(_userSaveData.Secret)
                && _userSaveData.Secret != "SECRET";
        }

        private void OnConnectionResponse(bool success, string error)
        {
            _spotify.ConnectionReceivedEvent -= OnConnectionResponse;
            ConnectionState = success ? ConnectionState.Connected : ConnectionState.Disconnected;

            if(!success)
            {
                MessageBox.Show($"Failed to connect with Spotify, received error: {error}. Please try again.", "Spotify Knob", MessageBoxButton.OK, MessageBoxImage.Error);

                AuthSaveDataHelper.CleanAuthData();
            }
            else
            {
                _spotify.StateRefreshed += OnStateRefreshed;

                _spotify.ProfileReceivedEvent += OnProfileReceived;
                _spotify.GetUserProfile();

                _spotify.CurrentStateReceivedEvent += OnCurrentStateReceived;
                _spotify.GetCurrentState();
            }
        }

        private void OnStateRefreshed()
        {
            CurrentVolume = _spotify.CurrentState.Device.VolumePercent.Value;
            _isPaused = !_spotify.CurrentState.IsPlaying;
        }

        private void OnCurrentStateReceived(bool success, string error)
        {
            _spotify.CurrentStateReceivedEvent -= OnCurrentStateReceived;


            if (!success)
            {
                MessageBox.Show($"Failed to get current state: {error}", "Spotify Knob", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (_spotify.CurrentState == null)
            {
                return; 
            }
            
            CurrentVolume = _spotify.CurrentState.Device.VolumePercent.Value;
        }

        private void OnProfileReceived(bool success, string error)
        {
            _spotify.ProfileReceivedEvent -= OnProfileReceived;

            if (success)
            {
                if(_spotify.Profile != null)
                {
                    ProfileName = _spotify.Profile.DisplayName;
                    ProfileImage = _spotify.Profile.Images.Count > 0 ? _spotify.Profile.Images[0].Url : "";
                }
            }
            else
            {
                MessageBox.Show($"Failed to get user profile: {error}", "Spotify Knob", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TurnVolumeDown()
        {
            ChangeCurrentVolume(CurrentVolume - 1);
            TriggerVolumeChange();
        }

        private void TurnVolumeUp()
        {
            ChangeCurrentVolume(CurrentVolume + 1);
            TriggerVolumeChange();
        }

        private void MuteUnmute()
        {
            if (_isMuted)
            {
                ChangeCurrentVolume(_unmuteVolume);
            }
            else
            {
                _unmuteVolume = CurrentVolume;
                ChangeCurrentVolume(0);
            }

            _spotify.ChangeVolume(CurrentVolume);
        }

        private void ChangeCurrentVolume(int volume)
        {
            CurrentVolume = Math.Max(0, Math.Min(100, volume));
        }

        private void TriggerVolumeChange()
        {
            if(_isVolumeOnCooldown)
            {
                return;
            }

            Task.Run(async () =>
            {
                _isVolumeOnCooldown = true;

                await Task.Delay(_volumeCooldown);

                _spotify.ChangeVolume(CurrentVolume);
                _isVolumeOnCooldown = false;
            });
        }

        private void PauseResume()
        {
            if(_isPaused)
            {
                _spotify.Resume();
            }
            else
            {
                _spotify.Pause();
            }

            _isPaused = !_isPaused;
        }
    }
}
