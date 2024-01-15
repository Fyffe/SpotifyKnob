using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyKnob.MVVM.Model
{
    enum ConnectionState : uint
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2
    }
}
