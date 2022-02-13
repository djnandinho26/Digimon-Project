﻿using System;
using System.Collections.Generic;
using Yggdrasil.Entities;
using Yggdrasil.Network;
using Yggdrasil.Packets;

namespace CharServer.Network
{
    public class CharClient : IUser
	{
		/// <summary>
        /// TCP connection.
        /// </summary>
		public IClient? Client { get; set; } = null;

        public uint AccountID;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int UniqueID;
        public int AccessLevel;
        public List<Character> Chars;
        
		public CharClient(IClient client)
		{
			Client = client;
		}

        public void SendHandShakeRes()
        {
            int time_t = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            short data = (short)(Client.Handshake ^ 0x7e41);
            PacketWriter writer = new PacketWriter();
            writer.Type(-2);
            writer.WriteShort(data);
            writer.WriteInt(time_t);
            Client.Send(writer.Finalize());
        }

        public void SendLoginResponse(byte[] data)
        {
            
        }

        public void SendServerList(bool isOnlyOne)
        {
            
        }

        public void SendCharacterLoad(byte[] data)
        {
            
        }

        public void SendCharacterCreate(byte[] data)
        {

        }

        public void SendDisconnect(byte[] data)
        {
           
        }

        public void SendConnectWaitCheckResult(byte[] data)
        {
            
        }

        public void SendConnectWaitCancelResult(byte[] data)
        {
           
        }

        public void SendCharacterSelectResult(byte[] data)
        {
           
        }
        
	}
}
