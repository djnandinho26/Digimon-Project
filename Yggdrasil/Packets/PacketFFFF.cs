using System.Reflection.Emit;

namespace Yggdrasil.Packets;

public class PacketFFFF : PacketWriter
{
    public PacketFFFF(short handshake)
    {
        Type(0xFFFF);
        WriteShort(handshake);
    }
}