using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyKnob.MVVM.Model
{
    enum HotkeyType : uint
    {
        None = 0,
        VolumeUp = 1,
        VolumeDown = 2,
        Mute = 3,
        Pause = 4
    }
}
