using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yggdrasil.Entities;

namespace Yggdrasil.Packets.Game
{
    public class DespawnPlayer : Packet
    {
        public DespawnPlayer(ushort hTamer, short hDigimon)
        {
            packet.Type(1006);
            packet.WriteShort(514);
            packet.WriteByte(0);
            packet.WriteUInt(hTamer);
            packet.WriteInt(hDigimon);
            packet.WriteByte(0);
        }
    }
}
