using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yggdrasil.Packets.Game
{
    public class RidingMode : Packet
    {
        public RidingMode(ushort hTamer, short hDigimon)
        {
            packet.Type(1325);
            packet.WriteUInt(hTamer);
            packet.WriteInt(hDigimon);
        }
    }
    public class StopRideMode : Packet
    {
        public StopRideMode(ushort hTamer, short hDigimon)
        {
            packet.Type(1326);
            packet.WriteUInt(hTamer);
            packet.WriteInt(hDigimon);
        }
    }
}
