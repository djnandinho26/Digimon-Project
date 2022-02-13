using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Yggdrasil.Network;
using Yggdrasil.Entities;
using Yggdrasil.Helpers;
using Yggdrasil.Packets;

namespace Yggdrasil.Network
{
	/// <summary>
	/// Description of IClient.
	/// </summary>
	public interface IClient
	{
		bool IsConnected { get; }
        IPEndPoint RemoteEndPoint { get; }
        IPEndPoint LocalEndPoint { get; }
        IUser User { get; set; }
        
        string Username { get; set; }
        string Password { get; set; }
        short Handshake { get; set; }
        Socket _Socket { get; }
        Character Tamer { get; set; }
        void Send(byte[] buffer);
        void Send(PacketWriter writer);
        void SendToAll(byte[] buffer);
        void SendToPlayer(string name, byte[] buffer);
        int Send(byte[] buffer, SocketFlags flags);
        int Send(byte[] buffer, int start, int count);
        int Send(byte[] buffer, int start, int count, SocketFlags flags);

        void Disconnect();
	}
}
