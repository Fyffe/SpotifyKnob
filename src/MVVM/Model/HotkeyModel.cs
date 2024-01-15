using SpotifyKnob.Core;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpotifyKnob.MVVM.Model
{
    class HotkeyModel : ObservableObject
    {
        public string DisplayName { get; set; } = "Default Hotkey";
        public HotkeyType HotkeyType { get; set; } = HotkeyType.None;
        public Action BoundAction { get; set; } = null;
        public Key BoundKey 
        { 
            get { return _boundKey; } 
            set { _boundKey = value; OnPropertyChanged(); } 
        }
        public int Cooldown { get; set; } = 0;
        public bool IsOnCooldown { get; set; } = false;

        public string BoundKeyReadable => BoundKey.ToString();

        private Key _boundKey = Key.None;

        public HotkeyModel()
        {

        }

        public HotkeyModel(HotkeyModel model)
        {
            DisplayName = model.DisplayName;
            HotkeyType = model.HotkeyType;
            BoundAction = model.BoundAction;
            _boundKey = model.BoundKey;
            Cooldown = model.Cooldown;
        }
    }
}
