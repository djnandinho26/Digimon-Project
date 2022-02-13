using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GameServer.Entities;
using Yggdrasil;
using GameServer.Database.Interfaces;
using Microsoft.Extensions.Configuration;
using Yggdrasil.Database;
using Yggdrasil.Entities;
using Yggdrasil.Helpers;
using Yggdrasil.Network;
using Yggdrasil.Packets;
using Yggdrasil.Packets.Game;
using Yggdrasil.Packets.Game.Chat;
using Yggdrasil.Packets.Game.Interface;
using Yggdrasil.Packets.Monster;

namespace GameServer.Network
{
    public class GameServ : Server
    {
        public Dictionary<string, IUser> Tamers = new();

        public List<MonsterEntity> MonstersEntity = new List<MonsterEntity>();

        public Dictionary<int, GameMap> Maps = new Dictionary<int, GameMap>();

        private readonly IGameDatabase _gameDatabase;
        private readonly IConfiguration _configuration;

        private string HostIP;
        private int HostPort;

        public GameServ(IConfiguration configuration, IGameDatabase gameDatabase)
        {
            OnConnect += GameServer_OnConnect;
            OnDisconnect += GameServer_OnDisconnect;
            DataReceived += GameServer_OnDataReceived;

            _configuration = configuration;
            _gameDatabase = gameDatabase;
        }

        private void GameServer_OnConnect(object sender, ClientEventArgs e)
        {
            SysCons.LogInfo("Client connected: {0}", e.Client.ToString());
            e.Client.User = new GameClient(e.Client);
            e.Client.Handshake = 0;
            e.Client.Send(new PacketFFFF(e.Client.Handshake)); 
        }

        private void GameServer_OnDisconnect(object sender, ClientEventArgs e)
        {
            var client = e.Client;
            if (client == null) return;
            Tamers.Remove(client.Tamer.Name);
            Maps[client.Tamer.Location.Map].Logout.Enqueue(client.Tamer);
            //Maps[client.Tamer.Location.Map].Leave(client);
            SysCons.LogInfo("Client disconnected: {0}", e.Client.ToString());
        }

        private void GameServer_OnDataReceived(object sender, ClientEventArgs e, byte[] data)
        {
            if (e.Client == null) return;
            PacketReader packet = new PacketReader(data);
            PacketProcess((GameClient)e.Client.User, packet);
        }

        public override void Run()
        {

            AchieveDB.Load("Data\\Achieve.bin");
            AddExpDB.Load("Data\\AddExp.bin");
            BattleTableDB.Load("Data\\BattleTable.bin");
            //BuffDB.Load("Data\\Buff.bin");
            CashShopDB.Load("Data\\CashShop.bin");
            //CharCreateTableDB.Load("Data\\CharCreateTable.bin");
            //CuidDBCuidDB.Load("Data\\Cuid.bin");
            //DMBaseDB.Load("Data\\DMBase.bin");
            Data_ExchangeDB.Load("Data\\Data_Exchange.bin");
            DigimonEvoDB.Load("Data\\DigimonEvo.bin");
            DigimonListDB.Load("Data\\DigimonList.bin");
            //DigimonParcelDB.Load("Data\\DigimonParcel.bin");
            Digimon_BookDB.Load("Data\\Digimon_Book.bin");
            //EffectListDB.Load("Data\\EffectList.bin");
            EventDB.Load("Data\\Event.bin");
            ExtraExchangeDB.Load("Data\\ExtraExchange.bin");
            GotchaDB.Load("Data\\Gotcha.bin");
            InfiniteWarDB.Load("Data\\InfiniteWar.bin");
            ItemListDB.Load("Data\\ItemList.bin");
            MapCharLightDB.Load("Data\\MapCharLight.bin");
            MapListDB.Load("Data\\MapList.bin");
            MapMonsterListDB.Load("Data\\MapMonsterList.bin", MonstersEntity);
            MapNpcDB.Load("Data\\MapNpc.bin");
            //MapObjectDB.Load("Data\\MapObject.bin");
            MapPortalDB.Load("Data\\MapPortal.bin");
            //MapRegionDB.Load("Data\\MapRegion.bin");
            MasterCardDB.Load("Data\\MasterCard.bin");
            MonstersDB.Load("Data\\Monster.bin", MonstersEntity);
            //NatureDB.Load("Data\\Nature.bin");
            NewTutorialDB.Load("Data\\NewTutorial.bin");
            New_ElementDB.Load("Data\\New_Element.bin");
            NpcDB.Load("Data\\Npc.bin");
            Parcing_WordDB.Load("Data\\Parcing_Word.bin");
            Passive_AbilityDB.Load("Data\\Passive_Ability.bin");
            QuestDB.Load("Data\\Quest.bin");
            RewardDB.Load("Data\\Reward.bin");
            RideDB.Load("Data\\Ride.bin");
            SceneDB.Load("Data\\Scene.bin");
            ServerTransferDB.Load("Data\\ServerTransfer.bin");
            SkillDB.Load("Data\\Skill.bin");
            //Spirit_NPCDB.Load("Data\\Spirit_NPC.bin");
            //SvLineUpDB.Load("Data\\SvLineUp.bin");
            TacticsDB.Load("Data\\Tactics.bin");
            TalkDB.Load("Data\\Talk.bin");
            TamerListDB.Load("Data\\TamerList.bin");
            //TimeChargeDB.Load("Data\\TimeCharge.bin");
            TutorialDB.Load("Data\\Tutorial.bin");
            //UITextDB.Load("Data\\UIText.bin");
            //WeatherDB.Load("Data\\Weather.bin");
            WorldMapDB.Load("Data\\WorldMap.bin");
            World();
            SpawnMonsters();
            Console.Title = "Game Server";
            var serverInformation = _configuration.GetSection("GameServer");
            HostIP = serverInformation["HostIP"];
            HostPort = int.Parse(serverInformation["HostPort"]);
            if (!this.Listen(HostIP, HostPort)) return;
            SysCons.LogInfo("GameServer is listening on {0}:{1}...", HostIP, HostPort);
        }

        public Character? FindClient(string Name)
        {
            return Tamers.TryGetValue(Name, out var tamer) ? tamer.Client?.Tamer : null;
        }

        public void SpawnMonsters()
        {

            foreach (MonsterEntity entity in MonstersEntity)
            {
                GameMap cMap = Maps[entity.Location.Map];

                cMap.Monsters.Add(entity);
            }
            SysCons.LogInfo("Monsters Loaded");
        }

        public Position GetMonsterPosition(int handle)
        {
            Position position = null;
            foreach (MonsterEntity entity in MonstersEntity)
            {
                if (entity.Handle == handle)
                {
                    position = entity.Location;

                    return position;
                }
            }
            return position;
        }
        public void Command(GameClient client, string[] cmd, Digimon Activemon)
        {
            if (client.AccessLevel <= 0) return;
            if (cmd.Length == 0) return;
            Character Tamer = client.Tamer;
            GameMap ActiveMap = Maps[client.Tamer.Location.Map];
            IClient Client = client.Client;
            switch (cmd[0])
            {
                case "item":

                    {
                        int fullId = 0;
                        int amount = 0;
                        try
                        {
                            fullId = int.Parse(cmd[1]);
                            amount = int.Parse(cmd[2]);
                        }
                        catch
                        {
                            Client.Send(new ChatNormal(Tamer.UID, string.Format("Enter id.")).ToArray());
                        }
                        Item e = new Item();
                        e.ItemId = fullId;
                        if (e.ItemData == null)
                        {
                            Client.Send(new ChatNormal(Tamer.UID, string.Format("An item with the id {0} was not found.", fullId)).ToArray());
                            return;
                        }

                        if (amount == null)
                        {
                            e.Amount = 1;
                        }
                        else
                        {
                            e.Amount = (ushort)amount;
                        }
                        if (e.ItemData.RemainingType > 0)
                        {
                            e.time_t = 1619828392;
                        }
                        else
                        {
                            e.time_t = -1;
                        }
                        int slot = Tamer.Inventory.FindSlot(e.ItemId);
                        if (slot != -1)
                        {
                            Client.Send(new CashWHItem(0, slot, e, e.Amount, e.Max).ToArray());
                            Tamer.Inventory[slot].Amount += e.Amount;
                        }
                        else
                        {
                            int iSlot = Tamer.Inventory.Add(e);
                            Client.Send(new CashWHItem(0, iSlot, e, e.Amount, e.Max).ToArray());
                        }
                        break;
                    }
                case "where":
                case "pos":
                case "loc":
                    {
                        Client.Send(new BaseChat(ChatType.Shout, "SERVER: ", string.Format("You are at {0}", Tamer.Location)).ToArray());
                        break;
                    }
                
                case "tamerset":
                    {
                        int value = 0;
                        if (!int.TryParse(cmd[2], out value)) return;
                        switch (cmd[1].ToLower())
                        {
                            case "level":
                            case "lv":
                                if (value <= 120)
                                {
                                    Tamer.Level = (int)value;
                                    ActiveMap.Send(new UpdateLevel(Tamer.UID, (byte)value).ToArray());
                                }
                                else
                                    return;
                                break;
                            case "exp":
                                Tamer.EXP = value; break;
                            case "at":
                                Tamer.AT = (int)value; break;
                            case "de":
                                Tamer.DE = (int)value; break;
                            case "hp":
                                Tamer.MaxHP = (int)value;
                                Tamer.HP = (int)value; break;
                            case "ds":
                                Tamer.MaxDS = (int)value;
                                Tamer.DS = (int)value; break;
                            case "fatigue":
                                Tamer.Fatigue = (int)value; break;
                            case "ms":
                                Tamer.MS = (short)value;
                                PacketWriter writer = new PacketWriter();
                                writer.Type(9905);
                                writer.WriteUInt(client.Tamer.UID);
                                writer.WriteUInt(client.Tamer.Partner.UID);
                                writer.WriteShort((short)value);
                                writer.WriteShort((short)value);
                                writer.WriteShort(0);
                                writer.WriteShort(0);
                                writer.WriteShort(0);
                                writer.WriteShort(0);
                                client.ActiveMap.Send(writer.Finalize());
                                break;
                            case "tamer":
                                Tamer.Model = (CharacterModel)value; break;
                            case "archive":
                                Tamer.ArchiveSize = (int)value; break;
                            case "inv":
                                if (value <= 150)
                                {
                                    Tamer.InventorySize = (int)value;
                                    Client.Send(new Inventory(Tamer).ToArray());
                                }
                                else
                                    return;
                                break;
                            case "storage":

                                if (value <= 315)
                                {
                                    Tamer.StorageSize = (int)value;
                                    Client.Send(new Storage(Tamer).ToArray());
                                }
                                break;
                            case "size":
                                ActiveMap.Send(new ChangeSize(Tamer.UID, (int)value, 0).ToArray());
                                //Tamer.Partner.Stats.MaxHP = (short)((decimal)Tamer.Partner.Stats.HP * ((ushort)value / 10000));
                                //Tamer.Partner.Stats.HP = (short)((decimal)Tamer.Partner.Stats.HP * ((ushort)value / 10000));
                                //Tamer.Partner.Stats.MaxDS = (short)((decimal)Tamer.Partner.Stats.DS * ((ushort)value / 10000));
                                //amer.Partner.Stats.DS = (short)((decimal)Tamer.Partner.Stats.DS * ((ushort)value / 10000));
                                //Tamer.Partner.Stats.DE = (short)((decimal)Tamer.Partner.Stats.DE * ((ushort)value / 10000));
                                //Tamer.Partner.Stats.CR = (short)((decimal)Tamer.Partner.Stats.CR * ((ushort)value / 10000));
                                //Tamer.Partner.Stats.AT = (short)((decimal)Tamer.Partner.Stats.AT * ((ushort)value / 10000));
                                Packet UpdateStats = new UpdateStats(Tamer, DigimonListDB.GetDigimon(Activemon.Species));
                                ActiveMap.Send(UpdateStats.ToArray());
                                break;
                            case "crowns":
                                if (value <= 999999999)
                                {
                                    client.crowns = value;
                                    Client.Send(new Crowns(value).ToArray());
                                }
                                break;
                            case "bits":
                                if (value <= 2147483647)
                                {
                                    Tamer.Money = value;
                                    Client.Send(new Inventory(Tamer).ToArray());
                                }
                                break;
                        }
                        Client.Send(new UpdateStats(Tamer, DigimonListDB.GetDigimon(Tamer.Partner.Species)).ToArray());
                        break;
                    }
                // case "digimon.set":
                case "digimonset":
                    {
                        if (cmd[1].ToLower() == "min")
                        {
                            Tamer.Partner.Stats = new DigimonStats();
                            Tamer.Partner.Level = 1;
                            Tamer.Partner.EXP = 0;
                            Client.Send(new UpdateStats(Tamer, Tamer.Partner).ToArray());
                            return;
                        }
                        else if (cmd[1].ToLower() == "max")
                        {
                            Tamer.Partner.Stats.Max();
                            Tamer.Level = 99;
                            Client.Send(new UpdateStats(Tamer, Tamer.Partner).ToArray());
                            return;
                        }
                        else if (cmd[1].ToLower() == "default")
                        {
                            DigimonData Data = DigimonListDB.GetDigimon(Tamer.Partner.CurrentForm);
                            if (Data != null)
                            {
                                Tamer.Partner.Stats = Data.Default(Tamer, Tamer.Partner.Stats.Intimacy, Tamer.Partner.Size);
                                Client.Send(new UpdateStats(Tamer, Tamer.Partner).ToArray());
                            }
                        }
                        // int value = 0;
                        //   Console.WriteLine(cmd[1].ToLower());
                        //  if (!int.TryParse(cmd[2], out value)) return;
                        //    Console.WriteLine(value);
                        //   switch (cmd[1].ToLower())
                        int value = 0;
                        if (!int.TryParse(cmd[2], out value)) return;
                        switch (cmd[1].ToLower())
                        {
                            case "lv":
                            case "level":
                                Activemon.Level = (int)value;
                                ActiveMap.Send(new UpdateLevel(Tamer.Partner.UID, (byte)value).ToArray());
                                break;
                            case "exp":
                                Tamer.Partner.EXP = value;
                                break;
                            case "hp":
                                Tamer.Partner.Stats.MaxHP = (short)value;
                                Tamer.Partner.Stats.HP = (short)value;
                                break;
                            case "ds":
                                Tamer.Partner.Stats.MaxDS = (short)value; Tamer.Partner.Stats.DS = (short)value; break;
                            case "at":
                                Tamer.Partner.Stats.AT = (short)value; break;
                            case "de":
                                Tamer.Partner.Stats.DE = (short)value; break;
                            case "ev":
                                Tamer.Partner.Stats.EV = (short)value; break;
                            case "ht":
                                Tamer.Partner.Stats.HT = (short)value; break;
                            case "cr":
                                Tamer.Partner.Stats.CR = (short)value; break;
                            case "as":
                                Tamer.Partner.Stats.AS = (short)value; break;
                            case "ms":
                                Tamer.Partner.Stats.MS = (short)value; break;
                            case "int":
                            case "sync":
                            case "intimacy":
                                Tamer.Partner.Stats.Intimacy = (short)value; break;
                            case "type":
                                Tamer.Partner.Species = (int)value;
                                Tamer.Partner.CurrentForm = (int)value;
                                break;
                            case "name":
                                Tamer.Partner.Name = cmd[2]; break;
                            case "size":
                                Tamer.Partner.Size = (short)value;
                                ActiveMap.Send(new ChangeSize((ushort)Activemon.UID, (int)value, 0).ToArray());
                                break;
                            case "scale":
                                Tamer.Partner.Scale = (byte)value; break;
                        }
                        DigimonData dData = DigimonListDB.GetDigimon(Activemon.CurrentForm);
                        if (dData != null)
                        {
                            Packet UpdateStats = new UpdateStats(Tamer, DigimonListDB.GetDigimon(Activemon.Species));
                            Tamer.Partner.Stats = dData.Default(Tamer, Tamer.Partner.Stats.Intimacy, Tamer.Partner.Size);
                            Client.Send(UpdateStats.ToArray());
                        }
                        break;
                    }
                case "rld":
                case "reload":
                    {
                        Client.Send(new MapChange(HostIP, HostPort,
                            Tamer.Location.Map, Tamer.Location.PosX, Tamer.Location.PosY, Tamer.Location.MapName).ToArray());
                        break;
                    }
                case "fix":
                    {
                        ActiveMap.Logout.Enqueue(client.Tamer);
                        Position ploc = new Position(3, 19996, 17810);
                        Client.Send(new MapChange(HostIP, HostPort,
                            ploc.Map, ploc.PosX, ploc.PosY, ploc.MapName).ToArray());
                        break;
                    }

                case "ann":
                    {
                        Client.SendToAll(new BaseChat().Megaphone(Tamer.Name, string.Join(" ", cmd, 1, cmd.Length - 1), 52, (short)Tamer.Level).ToArray());
                        break;
                    };
                case "notice":
                    {
                        Client.SendToAll(new BaseChat(ChatType.Whisper, string.Join(" ", cmd, 1, cmd.Length - 1)).ToArray());
                        break;
                    };
                case "unlockevo":
                    {
                        try
                        {
                            int evo = int.Parse(cmd[1]);
                            Tamer.Partner.Forms[evo].unlocked = 1;
                        }
                        catch
                        {

                        }
                        _gameDatabase.SaveDigimon(Tamer.Partner);
                        break;
                    }
                case "map":
                    {
                        int mapId = int.Parse(cmd[1]);
                        int X = int.Parse(cmd[2]);
                        int Y = int.Parse(cmd[3]);
                        MapData Map = MapListDB.GetMap(mapId);
                        Tamer.Location = new Position(mapId, X, Y);
                        _gameDatabase.SaveTamerPosition(client);
                        Client.Send(new MapChange(HostIP, HostPort, mapId, X, Y, Map.Name).ToArray());
                        break;
                    }
                case "dats":
                    MapData dats = MapListDB.GetMap(3);
                    var x = 19996;
                    var y = 17590;
                    Tamer.Location = new Position(3, x, y);
                    _gameDatabase.SaveTamerPosition(client);
                    Client.Send(new MapChange(HostIP, HostPort, 3, x, y, dats.Name).ToArray());
                    break;
                case "spawn":
                    {
                        var mobId = int.Parse(cmd[1]);
                        var digimon = DigimonListDB.GetDigimon(mobId);
                        var position = client.Tamer.Location;
                        Random rand = new Random();
                        var handle = 64999 + rand.Next(1, 5000) + 5000;
                        //ActiveMap.Monsters.Add
                        ActiveMap.Send(new MonsterSpawn(digimon, position, handle).ToArray());
                        break;
                    }
                case "serverxp":
                    var newExp = int.Parse(cmd[1]);
                    PacketWriter EXP = new PacketWriter();
                    EXP.Type(1054);
                    EXP.WriteInt(1);
                    EXP.WriteInt(newExp); // Server EXP rate
                    EXP.WriteInt(5);
                    EXP.WriteInt(0);
                    EXP.WriteInt(5500);
                    foreach (var map in Maps)
                    {
                        map.Value.Send(EXP.Finalize());
                    }
                    break;


                /*
                case "spawn":
                   {
                      uint value = 0;
                        uint.TryParse(cmd[1], out value);
                        Digimon Mob = _gameDatabase.GetDigimon(value);
                        if (Mob == null)
                        {
                           //client.Send(new BaseChat(ChatType.Normal, Tamer.DigimonUID, string.Format("Mob {0} was not found.", value)));
                        }
                       uint id = GetModel((uint)(64 + (Mob.Model * 128)) << 8);
                       GameMap cMap = Maps[Tamer.Location.Map];
                       //Map.Send(new SpawnObject(id, Tamer.Location.PosX, Tamer.Location.PosY));
                        break;
                    }*/
                case "save":
                    _gameDatabase.SaveClient(client);
                    _gameDatabase.SaveTamerPosition(client);
                    break;
                case "kill":
                    if (client.Tamer.TargetMonster == null)
                    {
                        SysCons.LogInfo("No target selected.");
                        return;
                    }
                    PacketWriter death = new PacketWriter();
                    death.Type(1021);
                    death.WriteInt(0);
                    death.WriteInt(client.Tamer.TargetMonster.Handle);
                    death.WriteInt(0); // target HP
                    death.WriteInt(2000); //probably skill damage
                    ActiveMap.Send(death.Finalize());
                    break;
            }
        }
        public PacketWriter applyBuff(Character Tamer, ushort BuffID, int time_t)
        {
            PacketWriter writer = new PacketWriter();
            writer.Type(4000);
            writer.WriteUInt(Tamer.UID);
            writer.WriteUShort(BuffID);
            writer.WriteInt(time_t);
            return writer;
        }
        public PacketWriter writeStats(Character Tamer, DigimonData data)
        {
            Digimon mon = Tamer.Partner;
            PacketWriter writer3 = new PacketWriter();
            writer3.Type(1043);
            writer3.WriteInt(Tamer.MaxHP);
            writer3.WriteInt(Tamer.MaxDS);
            writer3.WriteInt(Tamer.HP);
            writer3.WriteInt(Tamer.DS);
            writer3.WriteShort((short)Tamer.AT);
            writer3.WriteShort((short)Tamer.DE);
            writer3.WriteShort((short)Tamer.MS);
            writer3.WriteInt((short)(data.HP * (mon.Size / 10000) + mon.DigiClone_HP_Value));
            Tamer.Partner.Stats.MaxHP = (short)(data.HP * (mon.Size / 10000) + mon.DigiClone_HP_Value);
            Tamer.Partner.Stats.HP = (short)(data.HP * (mon.Size / 10000) + mon.DigiClone_HP_Value);
            writer3.WriteInt((short)(data.DS * (mon.Size / 10000)));
            writer3.WriteInt((short)(data.HP * (mon.Size / 10000) + mon.DigiClone_HP_Value));
            writer3.WriteInt((short)(data.DS * (mon.Size / 10000)));
            writer3.WriteShort(Tamer.Partner.Stats.Intimacy);
            writer3.WriteShort((short)(data.AT * (mon.Size / 10000) + mon.DigiClone_AT_Value));
            writer3.WriteShort((short)(data.DE * (mon.Size / 10000)));
            writer3.WriteShort((short)(data.CR * (mon.Size / 10000) + mon.DigiClone_CT_Value));
            writer3.WriteShort(data.AS);
            writer3.WriteShort(data.EV);
            writer3.WriteShort(data.HT);
            writer3.WriteShort(data.AR);
            writer3.WriteShort(data.BL);
            writer3.WriteShort((short)(1 + mon.DigiClone_AT + mon.DigiClone_BL + mon.DigiClone_CT + mon.DigiClone_EV + mon.DigiClone_HP)); // -----
            writer3.WriteShort(mon.DigiClone_AT_Value); //AT
            writer3.WriteShort(0); //--------
            writer3.WriteShort(mon.DigiClone_CT_Value); //CT
            writer3.WriteShort(0); //AS
            writer3.WriteShort(mon.DigiClone_EV_Value); //EV
            writer3.WriteShort(0);
            writer3.WriteShort(mon.DigiClone_HP_Value); //HP
            writer3.WriteShort(mon.DigiClone_AT); //CLONE EXAMPLE 15/15
            writer3.WriteShort(mon.DigiClone_CT); //CLONE EXAMPLE 15/15 ---
            writer3.WriteShort(mon.DigiClone_EV); //CLONE EXAMPLE 15/15
            writer3.WriteShort(0);
            writer3.WriteShort(0); //CLONE EXAMPLE 15/15
            writer3.WriteShort(0);
            writer3.WriteShort(mon.DigiClone_HP); //CLONE EXAMPLE 15/15 
            return writer3;
        }
        public static byte[] ToByteArray(String HexString)
        {
            int NumberChars = HexString.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
            }
            return bytes;
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
        public static uint GetModel(uint Model, byte Id)
        {
            uint hEntity = Model;
            return (uint)(hEntity + Id);
        }
        
        public static short GetHandle(uint Model, byte type)
        {
            byte[] b = new byte[] { (byte)((Model >> 32) & 0xFF), type };
            return BitConverter.ToInt16(b, 0);
        }
        



        /// <summary>
        /// Initialize GameMaps
        /// </summary>
        public void World()
        {
            foreach (KeyValuePair<int, MapData> kvp in MapListDB.MapList)
            {
                MapData Map = kvp.Value;
                GameMap gMap = new GameMap(Map.MapID);

                Maps.Add(gMap.MapId, gMap);
            }
            SysCons.LogInfo("GameMap Loaded");
        }


        public Dictionary<int, MapMonsterListDB> Mapmonsters = new Dictionary<int, MapMonsterListDB>();

        public void PacketProcess(GameClient client, PacketReader packet)
        {
            var Tamer = client.Tamer;
            Digimon ActiveMon = null;
            GameMap ActiveMap = null;
            Party ActiveParty = null;

            IClient Client = client.Client;

            PacketDefinitions.LogPacketData(packet);
            ActiveMon = Tamer.Partner;
            ActiveMap = Maps[client.Tamer.Location.Map];
            client.ActiveMap = Maps[client.Tamer.Location.Map];
            switch (packet.Type)
            {
                //11002 ACCEPT QUEST
                //11003 GIVE UP QUEST
                //11001 COMPLETED QUEST
                //10004 COMPLETE QUEST





                case -1:
                    {
                        PacketWriter writer = new PacketWriter();
                        client.time_t = (uint)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        short data = 0x7e41;
                        writer.Type(-2);
                        writer.WriteShort(data);
                        writer.WriteInt((int)client.time_t);
                        Client.Send(writer.Finalize());
                        break;
                    }
                case -3:
                    {
                        break;
                    }
                case 15: //equipping titles
                    {
                        short title = packet.ReadShort();
                        Packet Equip = new EquipAchievement(title, Tamer.IDX);
                        Tamer.CurrentTitle = title;
                        ActiveMap.Send(Equip.ToArray());
                        break;
                    }
                case 1001:
                    {

                        Digimon mon = Tamer.Partner;

                        PacketWriter Seals = new PacketWriter();
                        Seals.Type(1333);
                        Seals.WriteShort((short)Tamer.CurrentSealLeader);
                        Seals.WriteShort((short)Tamer.Seals.Count);
                        for (int i = 0; i < Tamer.Seals.Count; i++)
                        {
                            Seal seal = Tamer.Seals[i];
                            Seals.WriteBytes(seal.ToArray());
                        }
                        Client.Send(Seals.Finalize());
                        if (Tamer.XAI != 0)
                        {
                            XAI xai = ItemListDB.GetXAI(Tamer.XAI);
                            Packet XGaugeP = new XGauge(xai.XGauge, (short)xai.Unknown);
                            Packet XGauge2P = new XGauge(xai.Unknown, Tamer.XGauge);
                            Client.Send(Combine(XGaugeP.ToArray(), XGauge2P.ToArray()));
                        }
                        else
                        {
                            Packet XGauge1 = new XGauge(0, (short)0);
                            Packet XGauge2 = new XGauge(0, Tamer.XGauge);
                            Client.Send(Combine(XGauge1.ToArray(), XGauge2.ToArray()));
                        }


                        Packet Inventory = new Inventory(client.Tamer);
                        Packet Storage = new Storage(client.Tamer);
                        PacketWriter AccountS = new PacketWriter();
                        AccountS.Type(16038);
                        AccountS.WriteInt(0);
                        AccountS.WriteInt(0);
                        AccountS.WriteInt(0);
                        AccountS.WriteByte(2);
                        AccountS.WriteShort(14);
                        AccountS.WriteBytes(client.AccountStorage.ToArray());

                        Packet crowns = new Crowns(client.crowns);
                        PacketWriter EXP = new PacketWriter();
                        EXP.Type(1054);
                        EXP.WriteInt(1);
                        EXP.WriteInt(65500); // Server EXP rate
                        EXP.WriteInt(5);
                        EXP.WriteInt(0);
                        EXP.WriteInt(5500);
                        PacketWriter w2 = new PacketWriter();
                        w2.Type(3134);
                        w2.WriteInt(0);
                        w2.WriteInt(0);
                        w2.WriteInt(0);
                        PacketWriter w3 = new PacketWriter();
                        w3.Type(3990);
                        w3.WriteByte(1);
                        PacketWriter w4 = new PacketWriter();
                        w4.Type(9905);
                        w4.WriteUInt(Tamer.UID);
                        w4.WriteUInt(Tamer.Partner.UID);
                        w4.WriteShort(1493);
                        w4.WriteShort(1493);
                        w4.WriteInt(0);
                        w4.WriteInt(0);
                        PacketWriter w5 = new PacketWriter();
                        w5.Type(3133);
                        w5.WriteByte(0);
                        w5.WriteShort(14);
                        PacketWriter w6 = new PacketWriter();
                        w6.Type(3136);
                        w6.WriteShort(0);
                        PacketWriter w7 = new PacketWriter();

                        /*if (client.Membership != 0)
                        {
                            w7.Type(3414); //MEMBERSHIP
                            w7.WriteByte(1);
                            w7.WriteInt(1636710617);
                            Client.Send(Combine(w7.Finalize(), applyBuff(Tamer, 50121, 1636710617).Finalize(), applyBuff(Tamer, 50122, 1636710617).Finalize(), applyBuff(Tamer, 50123, 1636710617).Finalize()));
                        }
                        else if(client.Membership == 0)
                        {
                            w7.Type(3414); //MEMBERSHIP
                            w7.WriteByte(0);
                            w7.WriteInt(0);
                        }*/
                        w7.Type(3414); //MEMBERSHIP
                        w7.WriteByte(1);
                        w7.WriteInt(1636710617);
                        Client.Send(Combine(w7.Finalize(), applyBuff(Tamer, 50121, 1636710617).Finalize(), applyBuff(Tamer, 50122, 1636710617).Finalize(), applyBuff(Tamer, 50123, 1636710617).Finalize()));

                        PacketWriter w8 = new PacketWriter();
                        w8.Type(1040);
                        w8.WriteUInt(Tamer.UID);
                        w8.WriteByte(0);

                        Client.Send(Combine(EXP.Finalize(), w2.Finalize(), w3.Finalize(), w4.Finalize(), w5.Finalize(), w6.Finalize(), Inventory.ToArray(), Storage.ToArray(), AccountS.Finalize(), crowns.ToArray(), new Channels().ToArray()));
                        DigimonData data = DigimonListDB.GetDigimon(Tamer.Partner.CurrentForm);
                        if (data != null)
                        {
                            PacketWriter writer3 = writeStats(Tamer, data);
                            Client.Send(writer3.Finalize());
                        }

                        ActiveMap.Spawn(client.Tamer);

                        ActiveMap.SpawnMonsters(client.Tamer);

                        break;
                    }
                case 1004:
                    {
                        int unknown1 = packet.ReadInt();
                        int handle = packet.ReadInt();
                        int X = packet.ReadInt();
                        int Y = packet.ReadInt();
                        float dir = packet.ReadFloat();
                        if (handle == Tamer.UID)
                        {
                            Tamer.Location.PosY = Y;
                            Tamer.Location.PosX = X;
                        }
                        else if (handle == Tamer.Partner.UID)
                        {
                            Tamer.Partner.Location.PosX = X;
                            Tamer.Partner.Location.PosY = Y;
                        }
                        Maps[Tamer.Location.Map].Send(new MovePlayer(Tamer, X, Y).ToArray(), Tamer.UID);
                        break;
                    }
                case 1008: //CHATING ?
                    {

                        string text = packet.ReadString();

                        Packet ChatNormal = new ChatNormal(Tamer.UID, text);
                        SysCons.LogInfo("[{0}]: {1}", Tamer.Name, text);
                        if (text.StartsWith("."))
                        {
                            Command(client, text.Substring(1).Split(' '), ActiveMon);
                        }
                        else
                        {
                            ActiveMap.Send(ChatNormal.ToArray());
                        }
                        break;
                    }
                case 1009: //WHISPER
                    {
                        //12 00 68 09 -- Name -- byte 0
                        string player = packet.ReadString();
                        string message = packet.ReadString();
                        Character? other = FindClient(player);
                        if (other != null)
                        {
                            //other.Client.Send(new Whisper(Tamer.Name, player, message, 3).ToArray());
                            Client.Send(new Whisper(Tamer.Name, player, message, 2).ToArray());
                        }
                        else if (other == null)
                        {
                            Client.Send(new Whisper(Tamer.Name, player, message, 0).ToArray());
                        }
                        break;
                    }
                case 1013: //ATTACK //CRASHING
                    {
                        int digimonhandle = packet.ReadInt();
                        int entityhandle = packet.ReadInt();
                        var targetHp = ActiveMap.ReduceMonsterHealth(entityhandle, 20);
                        PacketWriter writer1 = new PacketWriter();
                        PacketWriter writer3 = new PacketWriter();
                        var monster = ActiveMap.Monsters.FirstOrDefault(x => x.Handle == entityhandle);
                        SysCons.LogInfo($"Attacking entity: {entityhandle} - EntityHP: {monster.HP}");
                        //SysCons.LogInfo(Packet.Visualize(packet.ToArray()));
                        //kill here
                        if (targetHp > 0)
                        {
                            ActiveMap.Send(Combine(new Battle(digimonhandle, entityhandle, 20, 0, 0).ToArray()));
                        }
                        else
                        {
                            PacketWriter death = new PacketWriter();
                            death.Type(1021);
                            death.WriteInt(0);
                            death.WriteInt(entityhandle);
                            death.WriteInt(0); // target HP
                            death.WriteInt(2000); //probably skill damage
                            ActiveMap.Send(death.Finalize());
                        }
                        break;
                    }
                case 1015:
                    {
                        SysCons.LogInfo("Trying to kill");
                        byte u1 = packet.ReadByte();
                        int digimonhandle = packet.ReadInt();
                        int entityhandle = packet.ReadInt();

                        PacketWriter writer = new PacketWriter();
                        PacketWriter writer10 = new PacketWriter();
                        PacketWriter writer3 = new PacketWriter();
                        PacketWriter writer4 = new PacketWriter();
                        PacketWriter writer5 = new PacketWriter();
                        PacketWriter writer6 = new PacketWriter();
                        PacketWriter writer7 = new PacketWriter();
                        PacketWriter writer8 = new PacketWriter();
                        writer.Type(1006);
                        writer.WriteInt(entityhandle);
                        writer.WriteInt(Tamer.Location.PosX);
                        writer.WriteInt(Tamer.Location.PosY);
                        writer.WriteInt(0);
                        writer10.Type(1034);
                        writer10.WriteInt(digimonhandle);
                        writer3.Type(1034);
                        writer3.WriteInt(entityhandle);
                        writer4.Type(1021);
                        writer4.WriteInt(digimonhandle);
                        writer4.WriteInt(entityhandle);
                        writer4.WriteInt(u1);
                        writer4.WriteInt(2000);
                        writer5.Type(1105);
                        writer5.WriteInt(0);
                        writer5.WriteByte(3);
                        writer5.WriteByte(3);
                        writer5.WriteByte(3);
                        writer5.WriteInt(1000);
                        writer6.Type(1018);
                        writer6.WriteInt(20300);
                        writer6.WriteInt(0);
                        writer6.WriteInt(366600);
                        writer6.WriteInt(0);
                        writer6.WriteInt(2108700);
                        writer6.WriteInt(digimonhandle);
                        writer6.WriteShort(7092);
                        writer6.WriteShort(3);
                        writer6.WriteInt(0);
                        writer6.WriteInt(3666600);
                        writer6.WriteInt(0);
                        writer6.WriteInt(24873400);
                        writer6.WriteInt(0);
                        writer6.WriteInt(11212);
                        writer7.Type(1035);
                        writer7.WriteInt(digimonhandle);
                        writer8.Type(1035);
                        writer8.WriteInt(entityhandle);
                        //ActiveMap.Send(new CombatOn(digimonhandle).ToArray());
                        //ActiveMap.Send(new CombatOn(entityhandle).ToArray());
                        //ActiveMap.Send(new UseSkill(digimonhandle, entityhandle, u1, (byte)50, 500).ToArray());
                        ActiveMap.Send((Combine(writer.Finalize(), writer10.Finalize(), writer3.Finalize(), writer.Finalize(), writer4.Finalize(), writer5.Finalize(), writer7.Finalize(), writer8.Finalize())));
                        ActiveMap.KillMonster(entityhandle);
                        break;
                    }
                case 1033:
                    {
                        PacketWriter writer = new PacketWriter();

                        writer.Type(1034);
                        writer.WriteUInt(Tamer.Partner.UID);
                        ActiveMap.Send(writer.Finalize());
                        break;
                    }
                case 1016:
                    {
                        int h1 = packet.ReadInt();
                        int h2 = packet.ReadInt();
                        if (h1 != Tamer.UID)
                        {
                            Tamer.Partner.CurrentForm = Tamer.Partner.Species;
                            _gameDatabase.SaveTamer(client);
                            PacketWriter writer = new PacketWriter();
                            writer.Type(1006);
                            writer.WriteShort(514);
                            writer.WriteByte(0);
                            writer.WriteUInt(Tamer.UID);
                            writer.WriteUInt(Tamer.Partner.UID);
                            writer.WriteByte(0);
                            Client.Send(writer.Finalize());
                        }

                        if (h2 != 0)
                        {
                            var target = ActiveMap.Monsters.FirstOrDefault(monster => monster.Handle == h2);
                            if (target != null)
                            {
                                SysCons.LogInfo($"Target found! {target.Name} [{target.Location.PosX},{target.Location.PosY}]");
                                Tamer.TargetMonster = target;
                                var monsterStats = MonstersDB.monsters[target.Species];
                                if (monsterStats != null)
                                {
                                    SysCons.LogInfo($"Monster stats found. {monsterStats.Name} {monsterStats.Level} {monsterStats.Tag}");
                                }
                            }
                            else
                            {
                                SysCons.LogInfo("Target entity was not found. (Maybe its a player?)");
                                Tamer.TargetMonster = null;
                            }
                        }
                        else
                        {
                            SysCons.LogInfo("Not targetting anything.");
                            Tamer.TargetMonster = null;
                        }
                        break;

                    }
                case 1018:
                    {
                        Client.Send(packet.ToArray());

                        break;
                    }
                case 1023:
                    {
                        int t_handle = packet.ReadInt();
                        short unknown = packet.ReadShort();
                        int unknown2 = packet.ReadInt();

                        PacketWriter writer = new PacketWriter();
                        writer.Type(1023);
                        writer.WriteUInt(Tamer.Partner.UID);
                        writer.WriteInt(unknown);
                        writer.WriteInt(unknown2);
                        break;
                    }
                case 1028:
                    {
                        int dhandle = packet.ReadInt();
                        int stage = packet.ReadByte();

                        SysCons.LogInfo(dhandle.ToString());
                        SysCons.LogInfo(Tamer.Partner.UID.ToString());
                        Digimon mon = Tamer.Partner;
                        EvolutionLine evolveLine = DigimonEvoDB.GetLine(mon.Species, mon.CurrentForm);

                        int evolutiontype = DigimonListDB.GetEvolutionType(evolveLine.Line[1][stage]);
                        switch (evolutiontype)
                        {
                            case 11:
                            case 12:
                            case 13:
                            case 14:
                            case 15:
                            case 16:
                                {
                                    XAI xai = ItemListDB.GetXAI(Tamer.XAI);
                                    Client.Send(new XGauge((short)xai.Unknown, Tamer.XGauge).ToArray());
                                    break;
                                }
                            case -1:
                                break;
                        }
                        mon.CurrentForm = evolveLine.Line[1][stage];
                        //mon.Model = GetModel(mon.ProperModel(), mon.byteHandle);
                        DigimonData data = DigimonListDB.GetDigimon(evolveLine.Line[1][stage]);
                        PacketWriter writer = new PacketWriter();
                        writer.Type(1028);
                        writer.WriteUInt(Tamer.DigimonUID);
                        writer.WriteUInt(Tamer.UID);
                        writer.WriteInt(evolveLine.Line[1][stage]);
                        writer.WriteByte((byte)stage);
                        writer.WriteShort(0xFF);
                        writer.WriteShort(0);
                        writer.WriteByte(0);
                        writer.WriteShort(6692);
                        writer.WriteShort(6694);
                        PacketWriter writer3 = writeStats(Tamer, data);
                        ActiveMap.Send(writer.Finalize());
                        Client.Send(writer3.Finalize());
                        break;
                    }
                case 1036:
                    {
                        byte u1 = packet.ReadByte();
                        int Slot = packet.ReadInt();
                        int NPC = packet.ReadInt();
                        TDBTactic egg = TacticsDB.Get(Tamer.Inventory[Slot].ItemId);
                        if (egg != null)
                        {
                            Tamer.Incubator = Tamer.Inventory[Slot].ItemId;
                            SysCons.LogInfo(egg.ItemId.ToString());
                            SysCons.LogInfo(egg.Species.ToString());
                            SysCons.LogInfo(egg.Data.ToString());
                            Tamer.IncubatorLevel = 0;
                            if (Tamer.Inventory[Slot].Amount > 1)
                            {
                                Tamer.Inventory[Slot].Amount -= 1;
                            }
                            else
                                Tamer.Inventory.Remove(Slot);
                        }
                        break;
                    }
                case 1037:
                    {
                        byte u1 = packet.ReadByte();
                        int slot = packet.ReadInt();
                        SysCons.LogInfo(slot.ToString());
                        TDBTactic eggg = TacticsDB.Get(Tamer.Incubator);
                        //int res = Opt.GameServer.HatchRates.Hatch(Tamer.IncubatorLevel);
                        Tamer.IncubatorLevel++;
                        if (Tamer.IncubatorLevel < 3)
                            Client.Send(new DataInputSuccess(Tamer.UID).ToArray());
                        else
                            Client.Send(new DataInputSuccess(Tamer.UID, (byte)Tamer.IncubatorLevel).ToArray());
                        break;
                    }
                case 1038:
                    {
                        byte u1 = packet.ReadByte();
                        int u2 = packet.ReadInt();
                        string digiName = packet.ReadString();
                        int NPCID = packet.ReadInt();
                        if (Tamer.IncubatorLevel < 3)
                            client.Client.Disconnect();
                        TDBTactic hatchegg = TacticsDB.Get(Tamer.Incubator);
                        if (hatchegg != null)
                        {
                            uint digiId = _gameDatabase.CreateMercenary(Tamer.CharacterId, digiName, hatchegg.Species, Tamer.IncubatorLevel,
14000, 0);
                            if (digiId == 0) return;
                            Digimon mon = _gameDatabase.GetDigimon(digiId);

                            for (int i = 0; i < Tamer.DigimonList.Length; i++)
                            {
                                if (Tamer.DigimonList[i] == null)
                                {
                                    Tamer.DigimonList[i] = mon;
                                    Client.Send(new Hatch(mon, i).ToArray());
                                    break;
                                }
                            }
                        }
                        Tamer.IncubatorLevel = 0;
                        Tamer.Incubator = 0;
                        _gameDatabase.SaveTamer(client);
                        break;
                    }
                case 1039:
                    {
                        //Remove Egg.
                        int NPCC = packet.ReadInt();
                        if (Tamer.IncubatorLevel == 0 && Tamer.Incubator != 0)
                        {
                            int slot = Tamer.Inventory.FindSlot((int)Tamer.Incubator);
                            if (slot != -1)
                            {
                                Tamer.Inventory[slot].Amount += 1;
                            }
                            else
                            {
                                Item e = new Item();
                                e.ItemId = Tamer.Incubator;
                                e.Amount = 1;
                                Tamer.Inventory.Add(e);
                            }
                        }
                        Tamer.Incubator = 0;
                        break;
                    }
                case 1041:// DIGIMON SWITCH
                    {
                        byte slot = packet.ReadByte();
                        Digimon target = Tamer.DigimonList[slot];
                        Digimon current = Tamer.DigimonList[0];


                        Packet UpdateStats = new UpdateStats(Tamer, DigimonListDB.GetDigimon(target.Species));
                        Packet Switch = new Switch(current.UID, slot, current, target);

                        Client.Send(UpdateStats.ToArray());
                        ActiveMap.Send(Switch.ToArray());

                        Tamer.Partner = target;
                        Tamer.DigimonList[slot] = current;
                        Tamer.DigimonList[slot].UID = Tamer.DigimonUID;
                        Tamer.DigimonList[0] = target;
                        break;
                    }
                case 1042:
                    {
                        break;
                    }
                case 1050: //Change Ch
                    {
                        int ch = packet.ReadInt();
                        //SqlDB.SaveTamer(client);
                        //client.Send(new MapChange(Opt.GameServer.IP.ToString(), Opt.GameServer.Port, Tamer.Location.Map, Tamer.Location.PosX, Tamer.Location.PosY, Tamer.Location.MapName));
                        //client.Send(new SendHandle(Tamer.UID));
                        break;
                    }
                case 1051:
                    _gameDatabase.SaveTamer(client);
                    break;
                case 1055:
                    {
                        Digimon Mon1 = Tamer.Partner;
                        SysCons.SavePacket(packet);
                        int unlocked_evo = packet.ReadInt();
                        short slot = packet.ReadShort();
                        if (slot <= 150)
                        {
                            PacketWriter writer = new PacketWriter();
                            writer.Type(1055);
                            writer.WriteInt(unlocked_evo);
                            Client.Send(writer.Finalize());
                        }

                        Mon1.levels_unlocked = unlocked_evo;
                        Mon1.Forms[unlocked_evo - 1].unlocked = 1;

                        _gameDatabase.SaveDigimon(Mon1);

                        break;
                    }
                case 1056:
                    {

                        string text = packet.ReadString();

                        Packet ChatShout = new BaseChat(ChatType.Shout, Tamer.Name, text);

                        ActiveMap.Send(ChatShout.ToArray());
                        break;
                    }
                case 1057: //JUMP BOOSTER
                    { 
                        break;
                    }
                case 1058: //EMOTICON
                    {
                        break;
                    }
                case 1075: //CLONING 
                    {


                        SysCons.SavePacket(packet);
                        int u1 = packet.ReadInt();
                        int slot = packet.ReadInt();
                        int time = packet.ReadInt();
                        if (Tamer.Inventory[slot].Amount > 1)
                        {
                            Tamer.Inventory[slot].Amount -= 1;
                        }
                        else
                        {
                            Tamer.Inventory.Remove(slot);
                        }
                        PacketWriter writer = new PacketWriter();

                        //WRONG PACKET
                        writer.Type(1075);
                        writer.WriteInt(3);
                        writer.WriteInt(1);
                        writer.WriteInt(1);

                        Client.Send(writer.Finalize());
                        break;
                    }
                case 1325:
                    {
                        Client.Send(new RidingMode(Tamer.UID, Tamer.DigimonUID).ToArray());
                        break;
                    }
                case 1326:
                    {
                        Client.Send(new StopRideMode(Tamer.UID, Tamer.DigimonUID).ToArray());
                        break;
                    }
                case 1501:
                    {
                        int handle = packet.ReadInt();
                        PacketWriter writer = new PacketWriter();
                        writer.Type(1508);
                        writer.WriteInt(handle);
                        Item Item = new Item();
                        writer.WriteBytes(Item.ToArray());
                        Client.Send(writer.Finalize());
                        break;
                    }
                case 1510: //SHOP LICENSE
                    {
                        break;
                    }
                case 1523: //SHOP STORAGE BOX
                    {
                        Client.Send(new ShopStorage().ToArray());
                        break;
                    }
                case 1703:
                    PacketWriter writer2 = new PacketWriter();

                    writer2.Type(1703);

                    Client.Send(writer2.Finalize());
                    break;
                case 1706:
                    {
                        uint u = packet.ReadUInt();
                        uint acctId = packet.ReadUInt();
                        int accessCode = packet.ReadInt();
                        _gameDatabase.LoadUser(client, acctId, accessCode);
                        _gameDatabase.LoadTamer(client);
                        Tamer.Client = Client;
                        Maps[client.Tamer.Location.Map].Login.Enqueue(client.Tamer);
                        break;
                    }
                case 1709:
                    {
                        int portalId = packet.ReadInt();
                        Portal Portal = MapPortalDB.GetPortal(portalId);
                        MapData Map = MapListDB.GetMap(Portal.MapId);

                        Tamer.Location = new Position(Portal);

                        Packet MapChange = new MapChange(HostIP, HostPort, Portal, Map.Name);
                        _gameDatabase.SaveTamer(client);
                        Client.Send(MapChange.ToArray());
                        break;
                    }
                case 1713:
                    {
                        PacketWriter writer = new PacketWriter();

                        writer.Type(1713);
                        writer.WriteShort(1);
                        writer.WriteByte(0xFF);
                        Client.Send(writer.Finalize());
                        break;
                    }
                case 2401: //ADD FRIEND
                    {
                        string friendname = packet.ReadString();
                        PacketWriter writer = new PacketWriter();
                        //writer.Type(2404);
                        //writer.WriteShort((short)tamer.friends.Count);
                        //writer.WriteBytes(tamer.friends.ToArray());
                        //writer.WriteShort(0);
                        writer.Type(2401);
                        writer.WriteByte(0);
                        writer.WriteString(friendname);
                        Client.Send(writer.Finalize());
                        //ToDO SQL function
                        break;
                    }
                case 2402: //DEL FRIEND
                    {
                        string friendname = packet.ReadString();
                        PacketWriter writer = new PacketWriter();
                        writer.Type(2402);
                        writer.WriteByte(0);
                        writer.WriteString(friendname);

                        //int slot = Tamer.friends.FindSlot(friendname);
                        //Tamer.friends.Remove(slot);
                        //Todo SQl function
                        Client.Send(writer.Finalize());
                        break;
                    }
                case 2403: //BLOCK FRIEND
                    {
                        string friendname = packet.ReadString();
                        PacketWriter writer = new PacketWriter();
                        writer.Type(2403);
                        writer.WriteByte(0);
                        writer.WriteString(friendname);
                        //Client.Send(new BlockFriend(friend).ToArray());
                        break;
                    }
                case 2405: //MEMO
                    {
                        break;
                    }


                // 2408 
                //12 00 68 09 -- Name -- byte 0
                case 3201:  //Packet Tamer no game ??? 
                    {
                        byte u1 = packet.ReadByte();
                        int Slot = packet.ReadInt();
                        int ArchiveSlot = packet.ReadInt() - 1000;
                        int uInt = packet.ReadInt();

                        if (Tamer.DigimonList[Slot] == null)
                        {
                            //Archive to Digivice
                            Digimon archiveDigimon = _gameDatabase.LoadDigimon(Tamer.ArchivedDigimon[ArchiveSlot]);
                            Tamer.ArchivedDigimon[ArchiveSlot] = 0;
                            Tamer.DigimonList[Slot] = archiveDigimon;

                        }
                        else if (Tamer.ArchivedDigimon[ArchiveSlot] == 0)
                        {
                            //Digivice to Archive
                            Digimon Mon1 = Tamer.DigimonList[Slot];
                            _gameDatabase.SaveDigimon(Mon1);

                            Tamer.ArchivedDigimon[ArchiveSlot] = Mon1.DigiId;
                            Tamer.DigimonList[Slot] = null;
                        }
                        else
                        {
                            //Swapping
                            Digimon Mon1 = Tamer.DigimonList[Slot];
                            _gameDatabase.SaveDigimon(Mon1);

                            Digimon Mon2 = _gameDatabase.LoadDigimon(Tamer.ArchivedDigimon[ArchiveSlot]);
                            Tamer.ArchivedDigimon[ArchiveSlot] = Mon1.DigiId;
                            Tamer.DigimonList[Slot] = Mon2;
                        }
                        _gameDatabase.SaveTamer(client);
                        Client.Send(new StoreDigimon(Slot, ArchiveSlot + 1000, 0).ToArray());
                        break;
                    }

                case 3204: //ARCHIVE ETC
                    {
                        Dictionary<int, Digimon> ArchivedDigimon = new Dictionary<int, Digimon>();
                        for (int i = 0; i < Tamer.ArchivedDigimon.Length; i++)
                        {
                            if (Tamer.ArchivedDigimon[i] == 0) continue;
                            Digimon dMon = _gameDatabase.LoadDigimon(Tamer.ArchivedDigimon[i]);
                            dMon.UID = Tamer.DigimonUID;
                            ArchivedDigimon.Add(i, dMon);
                        }
                        Client.Send(new Archive(Tamer.ArchiveSize, ArchivedDigimon).ToArray());
                        break;
                    }
                case 3132:
                    {
                        break;
                    }
                case 3136: //RECOMPENSAS LOGIN TIME
                    {
                        break;
                    }
                case 3232: // SEAL MASTER
                    {
                        short id = packet.ReadShort();
                        PacketWriter writer = new PacketWriter();
                        writer.Type(3232);
                        writer.WriteShort(id);
                        Client.Send(writer.Finalize());
                        break;
                    }
                case 3233: //DISMISS CURRENT LEADER
                    {
                        break;
                    }
                case 3239: //SPIRITS
                    {
                        int DigimonID = packet.ReadInt();
                        string Name = packet.ReadString();
                        ExtraExchanges exchange = ExtraExchangeDB.GetExchange(DigimonID);

                        uint digiId = _gameDatabase.CreateMercenary(Tamer.CharacterId, Name, DigimonID, 3,
9500, 0);
                        if (digiId == 0) return;
                        Digimon mon = _gameDatabase.GetDigimon(digiId);
                        for (int i = 0; i < Tamer.DigimonList.Length; i++)
                        {
                            if (Tamer.DigimonList[i] == null)
                            {
                                Tamer.DigimonList[i] = mon;
                                Client.Send(new Hatch(mon, i).ToArray());
                                break;
                            }
                        }

                        int SpiritCardSlot = -1;
                        int BokoSlot = -1;

                        for (int g = 0; g < exchange.ItemCount; g++)
                        {
                            int slot = Tamer.Inventory.FindSlot(exchange.requireditems[0][g]);
                            if (slot != -1)
                            {
                                SpiritCardSlot = slot;
                            }


                        }
                        for (int x = 0; x < exchange.MaterialCount; x++)
                        {
                            int slot = Tamer.Inventory.FindSlot(exchange.submaterials[0][x]);
                            if (slot != -1)
                            {
                                BokoSlot = slot;
                            }
                        }

                        PacketWriter writer = new PacketWriter();
                        writer.Type(3239);
                        writer.WriteInt(DigimonID);
                        int time_t = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        writer.WriteInt(time_t);
                        writer.WriteInt(0);
                        if (SpiritCardSlot != -1 && BokoSlot != -1)
                        {
                            writer.WriteByte(1);
                            writer.WriteInt(Tamer.Inventory[SpiritCardSlot].ItemId); //CART�O ESP�RITO USADO
                            writer.WriteByte(1);
                            writer.WriteInt(Tamer.Inventory[BokoSlot].ItemId); //BOKo USADO
                            if (Tamer.Inventory[SpiritCardSlot].Amount > 1)
                            {
                                Tamer.Inventory[SpiritCardSlot].Amount -= 1;
                            }
                            else
                                Tamer.Inventory.Remove(SpiritCardSlot);
                            if (Tamer.Inventory[BokoSlot].Amount > 1)
                            {
                                Tamer.Inventory[BokoSlot].Amount -= 1;
                            }
                            else
                                Tamer.Inventory.Remove(BokoSlot);
                        }
                        writer.WriteByte(0);
                        Client.Send(writer.Finalize());

                        break;
                    }
                case 3971:  //Abrir Seal ??
                    {
                        short slot = packet.ReadShort();
                        SysCons.LogInfo(slot.ToString());
                        Item Seal = Tamer.Inventory[slot];
                        MasterCards seal = MasterCardDB.Get((int)Seal.ItemId);
                        if (seal != null)
                        {
                            if (Seal.Amount > 0)
                            {
                                Tamer.Inventory.Remove(slot);
                                Seal newseal = new Seal();
                                newseal.ID = (short)seal.CardID;
                                newseal.Amount = (short)Seal.Amount;
                                newseal.u1 = seal.UID;
                                newseal.u2 = seal.U;
                                Tamer.Seals.Add(newseal);
                            }
                        }
                        break;
                    }
                case 3234: //Arquivo de enciclopedia ETC
                    {
                        Client.Send(new Encyclopedia().ToArray());
                        break;
                    }
                case 3404: //ALIVE?   VIVO?
                    {
                        break;
                    }
                case 3412:
                    {
                        PacketWriter writer = new PacketWriter();
                        writer.Type(3412);
                        writer.WriteShort(1);
                        writer.WriteByte(0);
                        Client.Send(writer.Finalize());
                        break;
                    }
                case 3413: //COMPRA ATRAVES DA CASH SHOP
                    {
                        int amount = packet.ReadByte();
                        int price = packet.ReadInt();
                        int type = packet.ReadInt();
                        int u1 = packet.ReadInt();
                        int[] unique_id = new int[amount];
                        bool checker = false;
                        for (int g = 0; g < amount; g++)
                        {
                            unique_id[g] = packet.ReadInt();
                            CASHINFO info = CashShopDB.getID(unique_id[g]);
                            if (info.Enabled == 1) { checker = true; }
                            else
                                checker = false;
                        }

                        if (checker == false)
                        {
                            PacketWriter writer = new PacketWriter();
                            writer.Type(3413);
                            writer.WriteShort(31012);
                            writer.WriteInt(0);
                            writer.WriteInt(0);
                            writer.WriteByte(0);
                            writer.WriteByte((byte)amount);
                            for (int c = 0; c < amount; c++)
                            {
                                writer.WriteInt(unique_id[c]);
                            }
                            Client.Send(writer.Finalize());
                            /*
                             * Server->Client
                            16 00 55 0D 24 79 00 00 00 00 00 00 00 00 00 01     ..U.$y..........
                            FC 5A 0D 0A 03 2A 1A   
                             * */
                        }
                        bool membership = false;
                        if (checker == true)
                        {
                            for (int u = 0; u < amount; u++)
                            {
                                CASHINFO data_ret = CashShopDB.getID(unique_id[u]);

                                if (data_ret != null || data_ret.Enabled == 1)
                                {
                                    for (int x = 0; x < data_ret.ItemCount; x++)
                                    {
                                        int slot = 0;
                                        Item e = new Item(0);
                                        e.ItemId = data_ret.Item[0][x];
                                        e.Amount = data_ret.Item[1][x];
                                        if (Tamer.crowns > price && type == 0)
                                        {
                                            client.crowns = client.crowns - price;
                                            //_gameDatabase.SaveClient(client);
                                            Packet s = new Crowns(client.crowns);
                                            PacketWriter writer = new PacketWriter();
                                            writer.Type(3413);
                                            writer.WriteByte((byte)amount);
                                            writer.WriteInt(price);
                                            writer.WriteInt(type);
                                            writer.WriteInt(u1);
                                            for (int g = 0; g < amount; g++)
                                            {
                                                if (unique_id[g] == 51010300)
                                                {
                                                    membership = true;
                                                }
                                                writer.WriteInt(unique_id[g]);
                                            }
                                            Client.Send(writer.Finalize());
                                            Client.Send(s.ToArray());
                                            checker = true;
                                        }
                                        //ENVIANDO PARA CASH-VAULT
                                        client.CashVault.Add(e);
                                    }
                                }
                            }

                        }

                        if (membership == true)
                        {
                            if (client.Membership != 0)
                            {
                                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(client.Membership);
                                dateTimeOffset.AddDays(30);

                                client.Membership = 1636710617;
                                PacketWriter writer = new PacketWriter();
                                writer.Type(3414); //MEMBERSHIP
                                writer.WriteByte(1);
                                writer.WriteInt(1636710617);
                                Client.Send(writer.Finalize());
                            }
                            else
                            {
                                DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
                                dateTimeOffset.AddDays(30);
                                client.Membership = 1636710617;
                                PacketWriter writer = new PacketWriter();
                                writer.Type(3414); //MEMBERSHIP
                                writer.WriteByte(1);
                                writer.WriteInt(1636710617);
                                Client.Send(writer.Finalize());
                            }

                        }

                        _gameDatabase.SaveClient(client);

                        break;
                    }
                case 3414:
                    {
                        PacketWriter writer = new PacketWriter();
                        writer.Type(3414);
                        writer.WriteByte(0);
                        writer.WriteInt(1607454000);
                        Client.Send(writer.Finalize());
                        break;
                    }
                case 3415:
                    {
                        Client.Send(new Cashshop(1).ToArray());
                        break;
                    }
                case 3901:
                    {
                        int tamerhandle = packet.ReadInt();
                        short slot = packet.ReadShort();
                        Item UsedItem = Tamer.Inventory[slot];
                        if (UsedItem.ItemId != 0 && UsedItem.ItemId != -1)
                        {
                            if (Tamer.Inventory[slot].Amount > 1)
                            {
                                Tamer.Inventory[slot].Amount -= 1;
                            }
                            else
                            {
                                Tamer.Inventory.Remove(slot);
                            }
                            PacketWriter writer = new PacketWriter();
                            writer.Type(3901);
                            writer.WriteInt(tamerhandle);
                            writer.WriteShort(slot);
                            switch (UsedItem.ItemData.Type)
                            {

                                case 9804: //GROWTH FRUIT
                                    {
                                        ActiveMap.Send(new ChangeSize(Tamer.DigimonUID, 12500, 0).ToArray());
                                        break;
                                    }

                                case 8917: //FRUITS
                                    {
                                        break;
                                    }
                                case 6300:
                                    {
                                        //BuffData data = BuffDB.GetBuff
                                        //ActiveMap.Send(applyBuff(Tamer, 100, ))
                                        break;
                                    }
                                case 15500:
                                    {
                                        Tamer.InventorySize += 1;
                                        Client.Send(Combine(new InventoryExpand(Tamer).ToArray(), writer.Finalize()));
                                        break;
                                    }
                                case 15600:
                                    {
                                        Tamer.StorageSize += 1;
                                        Client.Send(Combine(new StorageExpand(Tamer).ToArray(), writer.Finalize()));
                                        break;
                                    }
                                case 15900:
                                    {
                                        Tamer.mercenaryLimit += 1;
                                        Client.Send(Combine(new MercenarySlotExpand(Tamer).ToArray(), writer.Finalize()));
                                        break;
                                    }
                                case 16000:
                                    {
                                        Tamer.ArchiveSize += 1;
                                        Client.Send(Combine(new ArchiveExpand(Tamer).ToArray(), writer.Finalize()));
                                        break;
                                    }
                                case 17000:
                                    {
                                        Container container = ItemListDB.GetContainer((int)UsedItem.ItemId);
                                        if (container != null)
                                        {
                                            PacketWriter writercontainer = new PacketWriter();
                                            writercontainer.Type(3948);
                                            writercontainer.WriteInt((int)UsedItem.ItemId);
                                            writercontainer.WriteUInt(Tamer.UID);
                                            writercontainer.WriteShort(slot);
                                            switch (container.Type)
                                            {
                                                case 1:
                                                    {
                                                        int card = Random.Shared.Next(container.ItemCount);
                                                        writercontainer.WriteInt(1);
                                                        for (int f = 0; f < 1; f++)
                                                        {
                                                            Item newitem = new Item();
                                                            newitem.ItemId = container.Items_ItemID[card];
                                                            newitem.Amount = container.Items_Amount[card];
                                                            int slotid = Tamer.Inventory.FindSlot(container.Items_ItemID[card]);
                                                            if (slotid != -1)
                                                            {
                                                                ItemData data = ItemListDB.GetItem(container.Items_ItemID[card]);
                                                                if (data != null && Tamer.Inventory[slotid].Amount < data.Stack)
                                                                {
                                                                    Tamer.Inventory[slotid].Amount += newitem.Amount;
                                                                }
                                                                else if (data != null && Tamer.Inventory[slotid].Amount == data.Stack)
                                                                {
                                                                    Tamer.Inventory.Add(newitem);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Tamer.Inventory.Add(newitem);
                                                            }
                                                            writercontainer.WriteInt(container.Items_ItemID[card]);
                                                            writercontainer.WriteInt(container.Items_Amount[card]);
                                                            writercontainer.WriteInt(-1);
                                                        }
                                                        break;
                                                    }

                                                default:
                                                    {
                                                        writercontainer.WriteInt(container.ItemCount);
                                                        for (int f = 0; f < container.ItemCount; f++)
                                                        {
                                                            Item newitem = new Item();
                                                            newitem.ItemId = container.Items_ItemID[f];
                                                            newitem.Amount = container.Items_Amount[f];
                                                            int slotid = Tamer.Inventory.FindSlot(container.Items_ItemID[f]);
                                                            if (slotid != -1)
                                                            {
                                                                ItemData data = ItemListDB.GetItem(container.Items_ItemID[f]);
                                                                if (data != null && Tamer.Inventory[slotid].Amount < data.Stack)
                                                                {
                                                                    Tamer.Inventory[slotid].Amount += newitem.Amount;
                                                                }
                                                                else if (data != null && Tamer.Inventory[slotid].Amount == data.Stack)
                                                                {
                                                                    Tamer.Inventory.Add(newitem);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Tamer.Inventory.Add(newitem);
                                                            }
                                                            writercontainer.WriteInt(container.Items_ItemID[f]);
                                                            writercontainer.WriteInt(container.Items_Amount[f]);
                                                            writercontainer.WriteInt(-1);
                                                        }
                                                        break;
                                                    }
                                            }
                                            Client.Send(writercontainer.Finalize());
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        Client.Send(packet.ToArray());
                                        break;
                                    }


                            }
                        }
                        break;
                    }
                case 3904: //EQUIPAMENTOS E OUTROS
                    {
                        short Slot1 = packet.ReadShort();
                        short Slot2 = packet.ReadShort();

                        PacketWriter writer = new PacketWriter();
                        PacketWriter writer3 = new PacketWriter();
                        writer.Type(3904);
                        writer3.Type(1070);
                        writer3.WriteUInt(Tamer.UID);
                        writer3.WriteInt(0);
                        if (Slot1 >= 0 && Slot1 <= 150 && Slot2 >= 0 && Slot2 <= 150)
                        {
                            Item iSource = Tamer.Inventory[Slot1];
                            Item iDest = Tamer.Inventory[Slot2];

                            if (iSource.ItemId == iDest.ItemId)
                            {
                                //Combine Stacks
                                iDest.Amount += iSource.Amount;
                                Tamer.Inventory.Remove(Slot1);
                            }
                            else
                            {
                                //Switch items
                                Item iTemp = iSource;
                                Tamer.Inventory[Slot1] = iDest;
                                Tamer.Inventory[Slot2] = iSource;
                            }

                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Client.Send(writer.Finalize());
                        }
                        else if (Slot1 >= 1000 && Slot1 <= 1013 && Slot2 >= 0 && Slot2 <= 150)
                        {
                            int Slot = Slot1 - 1000;
                            Item Equip = Tamer.Equipment[Slot];
                            Item toEquip = Tamer.Inventory[Slot2];

                            if (Tamer.Inventory[Slot2].Amount != 0)
                            {
                                if (Slot1 == 1011)
                                {
                                    XAI xai = ItemListDB.GetXAI(toEquip.ItemId);
                                    Packet XGaugeP = new XGauge(xai.XGauge, (short)xai.Unknown);
                                    Tamer.XAI = toEquip.ItemId;
                                    if (Tamer.XGauge > xai.XGauge)
                                    {
                                        Tamer.XGauge = xai.XGauge;
                                    }
                                    Packet XGauge2P = new XGauge(xai.Unknown, Tamer.XGauge);
                                    Client.Send(Combine(XGaugeP.ToArray(), XGauge2P.ToArray()));
                                }
                                Tamer.Equipment[Slot] = toEquip;
                                Tamer.Inventory.Remove(Slot2);
                                Tamer.Inventory.Add(Equip, Slot2);
                            }
                            else
                            {
                                if (Slot1 == 1011)
                                {
                                    Packet XGaugeP = new XGauge(0, (short)0);
                                    Tamer.XAI = 0;
                                    Packet XGauge2P = new XGauge((short)0, 0);
                                    Client.Send(Combine(XGaugeP.ToArray(), XGauge2P.ToArray()));
                                }
                                Tamer.Inventory.Add(Equip);
                                Tamer.Equipment.Remove(Slot);
                            }
                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Packet UpdateEquipments = new UpdateEquipment(Tamer.IDX, 14);
                            Client.Send(Combine(writer3.Finalize(), writer.Finalize(), UpdateEquipments.ToArray()));

                        }
                        else if (Slot1 >= 2000 && Slot1 <= 2315 && Slot2 >= 2000 && Slot2 <= 2315)
                        {
                            int iSlot1 = Slot1 - 2000;
                            int iSlot2 = Slot2 - 2000;

                            Item iSource = Tamer.Storage[iSlot1];
                            Item iDest = Tamer.Storage[iSlot2];

                            if (iSource.ItemId == iDest.ItemId)
                            {
                                //Combine Stacks
                                iDest.Amount += iSource.Amount;
                                Tamer.Storage.Remove(iSlot1);
                            }
                            else
                            {
                                //Switch items
                                Tamer.Storage[iSlot1] = iDest;
                                Tamer.Inventory[iSlot2] = iSource;
                            }

                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Client.Send(writer.Finalize());

                        }
                        else if (Slot1 >= 5000 && Slot1 <= 5000 && Slot2 >= 0 && Slot2 <= 150)
                        {
                            Item Equip = Tamer.Equipment[14];
                            Item toEquip = Tamer.Inventory[Slot2];

                            if (Tamer.Inventory[Slot2].Amount != 0)
                            {
                                Tamer.Equipment[14] = toEquip;
                                Tamer.Inventory.Add(Equip, Slot2);
                            }
                            else
                                Tamer.Inventory.Add(Equip);
                            Tamer.Equipment.Remove(14);

                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Packet UpdateEquipments = new UpdateEquipment(Tamer.IDX, 14);
                            Client.Send(Combine(writer3.Finalize(), writer.Finalize(), UpdateEquipments.ToArray()));

                        }
                        else if (Slot1 >= 2000 && Slot1 <= 2315 && Slot2 >= 0 && Slot2 <= 150)
                        {
                            int Slot = Slot1 - 2000;
                            Item Source = Tamer.Storage[Slot];
                            if (Tamer.Inventory[Slot2].ItemId != 0)
                            {
                                Tamer.Inventory[Slot2].Amount += Source.Amount;
                            }
                            else
                            {
                                Tamer.Inventory[Slot2] = Source;
                            }
                            Tamer.Storage.Remove(Slot);
                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Client.Send(writer.Finalize());

                        }
                        else if (Slot1 >= 0 && Slot1 <= 150 && Slot2 >= 1000 && Slot2 <= 1013)
                        {
                            int Slot = Slot2 - 1000;



                            Item Equip = Tamer.Inventory[Slot1];
                            Tamer.Equipment[Slot] = Equip;

                            if (Slot1 == 1011)
                            {
                                XAI xai = ItemListDB.GetXAI(Equip.ItemId);
                                Packet XGaugeP = new XGauge(xai.XGauge, (short)xai.Unknown);
                                Tamer.XAI = Equip.ItemId;
                                Tamer.XGauge = xai.XGauge;
                                if (Tamer.XGauge > xai.XGauge)
                                {
                                    Tamer.XGauge = xai.XGauge;
                                }
                                Packet XGauge2P = new XGauge(xai.Unknown, Tamer.XGauge);
                                Client.Send(Combine(XGaugeP.ToArray(), XGauge2P.ToArray()));
                            }

                            Tamer.Inventory.Remove(Slot1);

                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Packet UpdateEquipments = new UpdateEquipment(Tamer.IDX, (byte)Slot);



                            Client.Send(Combine(writer3.Finalize(), writer.Finalize(), UpdateEquipments.ToArray()));

                        }
                        else if (Slot1 >= 0 && Slot1 <= 150 && Slot2 >= 2000 && Slot2 <= 2315)
                        {
                            int Slot = Slot2 - 2000;

                            Item Equip = Tamer.Inventory[Slot1];
                            Tamer.Storage[Slot] = Equip;
                            Tamer.Inventory.Remove(Slot1);
                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Client.Send(Combine(writer.Finalize()));

                        }

                        else if (Slot1 >= 0 && Slot1 <= 150 && Slot2 >= 4000 && Slot2 <= 4007)
                        {
                            int Slot = Slot2 - 4000;

                            Item Equip = Tamer.Inventory[Slot1];
                            Tamer.ChipSets[Slot] = Equip;
                            Tamer.Inventory.Remove(Slot1);

                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Packet UpdateEquipments = new UpdateEquipment(Tamer.IDX, (byte)Slot);



                            Client.Send(Combine(writer3.Finalize(), writer.Finalize(), UpdateEquipments.ToArray()));
                        }
                        else if (Slot1 >= 0 && Slot1 <= 150 && Slot2 >= 5000 && Slot2 <= 5000)
                        {
                            Item Equip = Tamer.Inventory[Slot1];
                            Item toInv = Tamer.Equipment[14];
                            Tamer.Equipment[14] = Equip;
                            Tamer.Inventory.Remove(Slot1);
                            if (toInv.ItemId != 0)
                            {
                                Tamer.Inventory.Add(toInv, Slot1);
                            }

                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Packet UpdateEquipments = new UpdateEquipment(Tamer.IDX, (byte)14);
                            Client.Send(Combine(writer3.Finalize(), writer.Finalize(), UpdateEquipments.ToArray()));

                        }
                        else if (Slot1 >= 0 && Slot1 <= 150 && Slot2 >= 3000 && Slot2 <= 3000)
                        {
                            int Slot = Slot2 - 3000;

                            Item Equip = Tamer.Inventory[Slot1];
                            Tamer.JogChipSet[Slot] = Equip;
                            Tamer.Inventory.Remove(Slot1);

                            writer.WriteShort(Slot1);
                            writer.WriteShort(Slot2);
                            Packet UpdateEquipments = new UpdateEquipment(Tamer.IDX, (byte)Slot);



                            Client.Send(Combine(writer3.Finalize(), writer.Finalize(), UpdateEquipments.ToArray()));
                        }
                        break;
                    }
                case 3907: //SPLITTING ITEMS
                    {
                        short Storage = 2000;
                        short Inventory = 150;
                        //Split

                        short sItemToSplit = packet.ReadShort();
                        short sDestination = packet.ReadShort();
                        byte sAmountToSplit = packet.ReadByte();

                        if (sItemToSplit >= Storage && sDestination >= Storage)
                        {
                            int ItemToSplit = Tamer.Storage.EquipSlot(sItemToSplit);
                            int Dest = Tamer.Storage.EquipSlot(sDestination);
                            Item iToSplit = Tamer.Storage[ItemToSplit];
                            Item iNew = iToSplit;
                            iNew.Amount = sAmountToSplit;
                            if (iToSplit.Amount == 0)
                            {
                                Tamer.Storage.Remove(sItemToSplit);
                            }
                            Tamer.Storage.Add(iNew, Dest);

                        }
                        else if (sItemToSplit <= Inventory & sDestination <= Inventory)
                        {
                            int ItemToSplit = Tamer.Inventory.EquipSlot(sItemToSplit);
                            int Dest = Tamer.Inventory.EquipSlot(sDestination);
                            Item iToSplit = Tamer.Inventory[ItemToSplit];
                            if (Tamer.Inventory.Count > Tamer.InventorySize) return; // LOOK FOR STOP THIS ACTION PACKET
                            Item iNew = new Item(0);
                            iNew.ItemId = iToSplit.ItemId;
                            iNew.time_t = iToSplit.time_t;
                            iNew.Amount = sAmountToSplit;
                            iToSplit.Amount -= sAmountToSplit;
                            if (iToSplit.Amount == 0)
                            {
                                Tamer.Inventory.Remove(sItemToSplit);
                            }

                            Tamer.Inventory.Add(iNew, Dest);
                        }
                        PacketWriter writer = new PacketWriter();
                        writer.Type(3907);
                        writer.WriteShort(sItemToSplit);
                        writer.WriteShort(sDestination);
                        writer.WriteByte(sAmountToSplit);
                        Client.Send(writer.Finalize());
                        break;

                    }
                case 3909: //
                    {
                        /*
                         Unknown Packet ID: 3909
12 00 45 0F 07 00 F4 35 00 00 D5 A6 00 00 01 00     ..E....5........
2E 1A  */
                        short slot = packet.ReadShort();

                        Tamer.Inventory.Remove(slot);

                        break;

                    }
                case 3923:
                    {
                        Client.Send(packet.ToArray());
                        break;
                    }
                case 3930:
                    {
                        PacketWriter writer = new PacketWriter();

                        writer.Type(3930);

                        short size = (short)client.CashVault.Count;


                        int counter = 0;

                        writer.WriteShort(size);

                        do
                        {
                            LoadTemp(client.CashVault[counter]);
                            counter++;

                        } while (counter < client.CashVault.Count);

                        //Packet loadtemp = new LoadTempVault(client);


                        void LoadTemp(Item x)
                        {
                            if (x.ItemId == 0)
                            {
                                int id = 0;
                                byte amount = 0;

                                writer.WriteInt(id);
                                writer.WriteInt(amount);
                                writer.WriteBytes(new byte[60]);
                            }
                            else
                            {
                                int id = x.ItemId;

                                int amount = x.Amount;

                                writer.WriteInt(id);
                                writer.WriteInt(amount);
                                writer.WriteBytes(new byte[60]);
                            }

                        }

                        Client.Send(writer.Finalize());



                        //Client.Send(loadtemp.ToArray());

                        //client.Send(packet);

                        break;

                    }
                case 3931: //TOINVENTORY
                    {
                        short slot = packet.ReadShort();
                        Item cashtoinv = client.CashVault[slot];

                        PacketWriter writer3 = new PacketWriter();
                        writer3.Type(3931);
                        int invslot = Tamer.Inventory.Add(cashtoinv);
                        writer3.WriteInt(invslot);
                        writer3.WriteInt(0);
                        writer3.WriteInt(slot);
                        writer3.WriteInt(cashtoinv.ItemId);
                        writer3.WriteInt(cashtoinv.Amount);
                        writer3.WriteByte(0);
                        writer3.WriteInt(cashtoinv.time_t);
                        writer3.WriteInt(0);

                        client.CashVault.RemoveFromCashWareHouse(slot, client.CashVault.Count);

                        PacketWriter writer = new PacketWriter();

                        writer.Type(3930);

                        short size = (short)client.CashVault.Count;


                        int counter = 0;

                        writer.WriteShort(size);

                        do
                        {
                            LoadTemp(client.CashVault[counter]);
                            counter++;

                        } while (counter < client.CashVault.Count);

                        //Packet loadtemp = new LoadTempVault(client);


                        void LoadTemp(Item x)
                        {
                            if (x.ItemId == 0)
                            {
                                int id = 0;
                                byte amount = 0;

                                writer.WriteInt(id);
                                writer.WriteInt(amount);
                                writer.WriteBytes(new byte[60]);
                            }
                            else
                            {
                                int id = x.ItemId;

                                int amount = x.Amount;

                                writer.WriteInt(id);
                                writer.WriteInt(amount);
                                writer.WriteBytes(new byte[60]);
                            }

                        }


                        Client.Send(Combine(writer3.Finalize(), writer.Finalize(), new Inventory(Tamer).ToArray()));
                        break;
                    }
                case 3935: //GIFT STORAGE
                    {
                        Client.Send(new GiftStorage(client.Tamer).ToArray());
                        break;
                    }
                case 3979: //REPURCHASE
                    {
                        Client.Send(new Repurchase().ToArray());
                        break;
                    }
                case 3986:
                    {
                        int type = packet.ReadByte();
                        switch (type)
                        {
                            case 2:
                                PacketWriter AccountS = new PacketWriter();
                                AccountS.Type(16038);
                                AccountS.WriteInt(0);
                                AccountS.WriteInt(0);
                                AccountS.WriteInt(0);
                                AccountS.WriteByte(2);
                                AccountS.WriteShort(14);
                                AccountS.WriteBytes(client.AccountStorage.ToArray());
                                break;
                            case 1:
                                Client.Send(new Storage(client.Tamer).ToArray());
                                break;
                            case 0:
                                Client.Send(new Inventory(client.Tamer).ToArray());
                                break;

                        }
                        break;
                    }
                case 3987: //SCANNING
                    {
                        int id = packet.ReadByte();
                        int u1 = packet.ReadInt();
                        int NPCID = packet.ReadInt();
                        int slot = packet.ReadInt(); //Inventory Slot
                        short amount = packet.ReadShort(); // Amount to give
                        PacketWriter writer = new PacketWriter();
                        Item UsedItem = Tamer.Inventory[slot];
                        int[][] ScannedItems = new int[][] { new int[amount], new int[amount] };
                        Container container = ItemListDB.GetContainer(UsedItem.ItemId);
                        writer.Type(3987);
                        writer.WriteInt(0);
                        writer.WriteInt(-1);
                        writer.WriteInt(-1);
                        int time_t = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        writer.WriteInt(time_t);
                        writer.WriteInt(0);
                        writer.WriteInt(slot);
                        writer.WriteInt(UsedItem.ItemId);
                        writer.WriteShort((short)amount);
                        writer.WriteInt(amount);
                        for (int u = 0; u < amount; u++)
                        {
                            int card = Random.Shared.Next(0, container.ItemCount);
                            ScannedItems[0][u] = container.Items_ItemID[card];
                            ScannedItems[1][u] = container.Items_Amount[card];
                        }
                        for (int g = 0; g < amount; g++)
                        {
                            if (Tamer.Inventory[slot].Amount > 1)
                            {
                                Tamer.Inventory[slot].Amount -= 1;
                            }
                            else
                            {
                                Tamer.Inventory.Remove(slot);
                            }
                            Item newitem = new Item();
                            int scanneditemslot = 0;
                            newitem.ItemId = ScannedItems[0][g];
                            newitem.Amount = ScannedItems[1][g];
                            newitem.time_t = -1;
                            int slotid = Tamer.Inventory.FindSlot(ScannedItems[0][g]);
                            if (slotid != -1)
                            {
                                if (Tamer.Inventory[slotid].Amount < Tamer.Inventory[slotid].ItemData.Stack)
                                {
                                    Tamer.Inventory[slotid].Amount += newitem.Amount;
                                    scanneditemslot = slotid;
                                    Tamer.Money -= Tamer.Inventory[slotid].ItemData.Buy;
                                }
                                else if (Tamer.Inventory[slotid].Amount == Tamer.Inventory[slotid].ItemData.Stack)
                                {
                                    scanneditemslot = Tamer.Inventory.Add(newitem);
                                    Tamer.Money -= Tamer.Inventory[slotid].ItemData.Buy;
                                }
                            }
                            else
                            {
                                scanneditemslot = Tamer.Inventory.Add(newitem);
                            }
                            writer.WriteInt(scanneditemslot);
                            writer.WriteInt(newitem.ItemId);
                            writer.WriteInt(newitem.Amount);
                            writer.WriteBytes(new byte[52]);
                            writer.WriteInt(0xFFFF);
                            writer.WriteInt(0);
                        }
                        Client.Send(writer.Finalize());
                        Client.Send(new Inventory(Tamer).ToArray());


                        /*
                        if (UsedItem.ItemId != 0 && UsedItem.ItemId != -1)
                        {
                            Container container = ItemDB.GetContainer((int)UsedItem.ItemId);
                            if (container != null)
                            {
                                if (Tamer.Inventory[slot].Amount > 1)
                                {
                                    Tamer.Inventory[slot].Amount -= 1;
                                }
                                else
                                {
                                    Tamer.Inventory.Remove(slot);
                                }
                                PacketWriter writer = new PacketWriter();
                                PacketWriter writer5 = new PacketWriter();
                                writer.Type(3987);
                                writer.WriteInt(0);
                                writer.WriteInt(-1);
                                writer.WriteInt(-1);
                                int time_t = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                writer.WriteInt(time_t);
                                writer.WriteInt(0);
                                writer.WriteInt(slot);
                                writer.WriteInt(UsedItem.ItemId);
                                writer.WriteShort((short)amount);
                                writer.WriteInt(amount);
                                Item newitem = new Item();
                                switch (container.Type)
                                {
                                    case 1:
                                        {
                                            int scanneditemslot = 0;
                                            int card = Rand.Next(container.ItemCount);
                                            newitem.ItemId = container.Items_ItemID[card];
                                            newitem.Amount = container.Items_Amount[card];
                                            int slotid = Tamer.Inventory.FindSlot(container.Items_ItemID[card]);
                                            if (slotid != -1)
                                            {
                                                ItemData data = ItemDB.GetItem(container.Items_ItemID[card]);
                                                if (data != null && Tamer.Inventory[slotid].Amount < data.Stack)
                                                {
                                                    Tamer.Inventory[slotid].Amount += newitem.Amount;
                                                    Tamer.Money = Tamer.Money - data.Buy;
                                                }
                                                else if (data != null && Tamer.Inventory[slotid].Amount == data.Stack)
                                                {
                                                    scanneditemslot = Tamer.Inventory.Add(newitem);
                                                    Tamer.Money = Tamer.Money - data.Buy;
                                                }
                                            }
                                            else
                                            {
                                                scanneditemslot = Tamer.Inventory.Add(newitem);
                                            }
                                            writer.WriteInt(scanneditemslot);
                                            writer.WriteInt(newitem.ItemId);
                                            writer.WriteInt(newitem.Amount);
                                            writer.WriteBytes(new byte[52]);
                                            writer.WriteInt(0xFFFF);
                                            writer.WriteInt(0xFFFF);
                                            break;
                                        }
                                }
                                Client.Send(writer.Finalize());
                                if (newitem.ItemId == 44056 || newitem.ItemId == 44056)
                                {
                                    writer5.Type(16034);
                                    writer5.WriteShort(3);
                                    writer5.WriteString(Tamer.Name);
                                    writer5.WriteInt(container.ItemID);
                                    writer5.WriteInt(newitem.ItemId);
                                    Client.SendToAll(writer5.Finalize());
                                }
                            }
                        }*/

                        break;
                    }
                //11002 ACCEPT QUEST
                //11003 GIVE UP QUEST
                //11001 COMPLETED QUEST
                //10004 COMPLETE QUEST
                case 11002:
                    {
                        short questid = packet.ReadShort();

                        Quest quest = new Quest();
                        quest.QuestId = questid;
                        Tamer.Quests.Add(quest);
                        PacketWriter writer = new PacketWriter();
                        writer.Type(16038);
                        writer.WriteInt(0);
                        writer.WriteByte(225);
                        writer.WriteInt(0);
                        writer.WriteInt(0);
                        //Client.Send(writer.Finalize());
                        break;
                    }
                case 11007: //GET ACHIEVEMENT
                    {
                        //SysCons.LogInfo(Packet.Visualize(packet.ToArray()));

                        break;
                    }
                case 16001: //REWARD STORAGE
                    {
                        PacketWriter writer = new PacketWriter();
                        writer.Type(16050);
                        writer.WriteInt(0);
                        Client.Send(Combine(new RewardStorage(client.Tamer).ToArray(), writer.Finalize()));

                        break;
                    }
                default:
                    {
                        //Unknown Packets!
                        PacketDefinitions.LogPacketData(packet);
                    }
                    break;


            }



        }
    }
}
