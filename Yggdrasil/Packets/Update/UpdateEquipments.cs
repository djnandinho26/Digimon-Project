using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yggdrasil.Entities;

namespace Yggdrasil.Packets.Game
{
    public class UpdateEquipment : Packet
    {
        /// <summary>
        /// Update Model?
        /// </summary>
        /// <param name="Slot"></param>
        public UpdateEquipment(short InvSlot, short Slot)
        {
            packet.Type(1310);
            packet.WriteInt(InvSlot);
            packet.WriteInt(Slot);
            packet.WriteInt(0);
            packet.WriteInt(0);
            packet.WriteShort(0);
        }
    }
}