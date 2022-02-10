using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yggdrasil.Entities;

namespace Yggdrasil.Packets.Game
{
    public class SpawnPlayer:Packet
    {
        public SpawnPlayer(Character Tamer)
        {
            Digimon Partner = Tamer.Partner;
            packet.Type(1006);
            packet.WriteShort(259);
            packet.WriteByte(0);
            packet.WriteInt(Tamer.Location.PosX);
            packet.WriteInt(Tamer.Location.PosY);
            packet.WriteUInt(Tamer.TamerHandle);
            packet.WriteInt((int)Tamer.Model);
            packet.WriteInt(Tamer.Location.PosX);
            packet.WriteInt(Tamer.Location.PosY);
            packet.WriteString(Tamer.Name);
            packet.WriteByte((byte)Tamer.Level);
            packet.WriteUInt(Tamer.intHandle);
            packet.WriteShort((short)Tamer.MS);
            packet.WriteByte(0xff);
            for (int i = 0; i < 15; i++)
                packet.WriteBytes(Tamer.Equipment[i].ToArray());
            packet.WriteInt(0);
            packet.WriteInt(1);
            packet.WriteInt(Tamer.DigimonHandle);
            packet.WriteShort(10000);
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteShort(32261);
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteByte(0);
        }

        public SpawnPlayer(Digimon Partner, ushort Handle)
        {
            packet.Type(1006);
            packet.WriteShort(259);
            packet.WriteByte(0);
            packet.WriteInt(Partner.Location.PosX);
            packet.WriteInt(Partner.Location.PosY);
            packet.WriteInt(Partner.Handle);
            packet.WriteUInt(Partner.Model);
            packet.WriteInt(Partner.Location.PosX);
            packet.WriteInt(Partner.Location.PosY);
            packet.WriteString(Partner.Name);
            packet.WriteByte((byte)Partner.Level);
            packet.WriteUInt(Partner.Model);
            packet.WriteShort((short)Partner.Stats.MS);
            packet.WriteByte(0xff);
            packet.WriteInt(0);
            packet.WriteShort(0);
        }

        public SpawnPlayer(Character Tamer, ushort TamerHandle)
        {
            packet.Type(1006);
            packet.WriteByte(3);
            packet.WriteShort(2);
            packet.WriteInt(Tamer.Partner.Location.PosX);
            packet.WriteInt(Tamer.Partner.Location.PosY);
            packet.WriteInt(Tamer.Partner.Handle);
            packet.WriteInt(Tamer.Partner.CurrentForm);
            packet.WriteInt(Tamer.Partner.Location.PosX);
            packet.WriteInt(Tamer.Partner.Location.PosY);
            packet.WriteString(Tamer.Partner.Name);
            packet.WriteShort(Tamer.Partner.Size);
            packet.WriteByte((byte)Tamer.Partner.Level);
            packet.WriteUInt(Tamer.Partner.Model);
            packet.WriteShort(Tamer.Partner.Stats.MS);
            packet.WriteShort(Tamer.Partner.Stats.MS);
            packet.WriteInt(Tamer.TamerHandle);
            packet.WriteByte(255);
            packet.WriteInt(0);
            packet.WriteInt(1);
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteInt(Tamer.Location.PosX);
            packet.WriteInt(Tamer.Location.PosY);
            packet.WriteInt(Tamer.TamerHandle);
            packet.WriteInt((int)Tamer.Model);
            packet.WriteInt(Tamer.Location.PosX);
            packet.WriteInt(Tamer.Location.PosY);
            packet.WriteString(Tamer.Name);
            packet.WriteByte((byte)Tamer.Level);
            packet.WriteUInt(Tamer.intHandle);
            packet.WriteShort((short)Tamer.MS);
            packet.WriteByte(255);
            packet.WriteBytes(Tamer.Equipment.ToArray());
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteInt(Tamer.Partner.Handle);
            packet.WriteShort(10000);
            packet.WriteShort(0);
            packet.WriteByte(0);
            packet.WriteString(Tamer.GuildName);
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteByte(0);
        }
    }
}
