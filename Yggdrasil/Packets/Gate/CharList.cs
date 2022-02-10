using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yggdrasil.Entities;

namespace Yggdrasil.Packets.Game
{
    public class CharList : Packet
    {
        public CharList(List<Character> listTamers)
        {
            packet.Type(1301);
            foreach (Character Tamer in listTamers)
            {
                packet.WriteByte((byte)Tamer.CharacterPos);
                packet.WriteShort((short)Tamer.Location.Map);
                packet.WriteInt((int)Tamer.Model);
                packet.WriteByte((byte)Tamer.Level);
                packet.WriteString(Tamer.Name);
                for (int i = 0; i < 14; i++)
                {
                    Item item = Tamer.Equipment[i];
                    packet.WriteBytes(item.ToArray());
                }
                packet.WriteInt(Tamer.Partner.Species);
                packet.WriteByte((byte)Tamer.Partner.Level);
                packet.WriteString(Tamer.Partner.Name);
                packet.WriteByte(1);
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteByte(0);
            }
            packet.WriteByte(99);
        }

    }
}
