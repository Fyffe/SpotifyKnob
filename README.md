![Logo](https://raw.githubusercontent.com/Fyffe/SpotifyKnob/master/src/Images/SpotifyKnob_128.png)

# Spotify Knob

A simple, lightweight app to control your Spotify playback with keyboard hotkeys. Made in about 8 hours total just to try out the WPF so expect minor bugs.

## Features

You can set up hotkeys for:
- volume up
- volume down
- mute/unmute
- pause/resume

## How to use

1. Go to https://developer.spotify.com/dashboard
2. Press the `Create app` button
3. Fill out `app name` and `app description`
4. Set website and redirect URI to `http://localhost:5543/callback`
5. Select `Web Playback SDK` and `Web API` in `APIs used` and press `Save`
6. Open the location of Spotify Knob on your PC and edit the `User.xml` file
7. In your newly created app go to `Settings` and press the `View client secret` text
8. Set the `ClientId` in `User.xml` to your app's `Client Id`
9. Set the `Secret` in `User.xml` to your app's `Client secret`
10. Set your username to whatever :)
11. Launch the app, press `Connect` and authorize the application - it's good to go

## Wishlist

Maybe some time in the future:
- hotkeys with modifiers
- actual knob to change the volume (just for fun)
