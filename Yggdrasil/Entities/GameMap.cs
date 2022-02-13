using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Yggdrasil.Helpers;
using Yggdrasil.Packets;
using Yggdrasil.Packets.Game;

namespace Yggdrasil.Entities;

public class GameMap
{
    public int MapId = 0;
    public Dictionary<string, Character> Tamers = new();
    public List<MonsterEntity> Monsters = new List<MonsterEntity>();
    public List<uint> UsedUid { get; set; } = new();
    private Thread tMonitor = null;
    private Thread cMonsters = null;
    public int[] Handlers;
    public GameMap(int MapId)
    {
        this.MapId = MapId;
        tMonitor = new Thread(new ThreadStart(Monitor));
        tMonitor.IsBackground = true;
        tMonitor.Start();
    }

public void SpawnMonsters(Character tamer)
{
    PacketWriter writer = new PacketWriter();

    List<MonsterEntity> AliveMonsters = new List<MonsterEntity>();

    foreach (MonsterEntity entity in Monsters)
    {
        if (entity.isAlive)
        {
            AliveMonsters.Add(entity);
        }
    }

    if (AliveMonsters.Count != 0 || AliveMonsters.Count != null)
    {
        writer.Type(1006);
        writer.WriteByte(3);
        writer.WriteShort((short)AliveMonsters.Count);
        for (int i = 0; i < AliveMonsters.Count; i++)
        {
            MonsterEntity monster = AliveMonsters[i];
            writer.WriteInt(monster.Location.PosX);
            writer.WriteInt(monster.Location.PosY);
            writer.WriteInt(monster.Handle);
            writer.WriteInt(monster.Species);
            writer.WriteInt(monster.Location.PosX + monster.Collision);
            writer.WriteInt(monster.Location.PosY);
            writer.WriteByte(0xff);
            writer.WriteShort((short)monster.Level);
            writer.WriteShort(2);
            writer.WriteInt(0);
            writer.WriteInt(0);
            writer.WriteInt(0);
            writer.WriteByte(0);
        }
    }
    else
    {
        return;
    }
    writer.WriteInt(0);
    tamer.Client?.Send(writer.Finalize());
}

public void Enter(Character Tamer)
{
    Tamers.Add(Tamer.Name, Tamer);
}

public void Leave(Character Tamer)
{
    Tamers.Remove(Tamer.Name);
    Send(new DespawnPlayer(Tamer.UID, Tamer.DigimonUID).ToArray());
}

public Queue<Character> Login { get; set; } = new();
public Queue<Character> Logout { get; set; } = new();

private void Monitor()
{
    while (true)
    {
        lock (Logout)
        {
            var tamers = Logout.DequeueChunk(Logout.Count).ToList();
            foreach (var tamer in tamers)
            {
                UsedUid.Remove(tamer.UID);
                UsedUid.Remove(tamer.DigimonUID);
                Leave(tamer);
            }
        }
        lock (Login)
        {
            var tamers = Login.DequeueChunk(Login.Count).ToList();
            foreach(var tamer in tamers)
            {
                tamer.UID = (uint) Random.Shared.Next(1 << 14, 2 << 14);
                tamer.DigimonUID = (uint) Random.Shared.Next(1 << 14, 2 << 14);
                while (UsedUid.Contains(tamer.UID))
                {
                    tamer.UID = (uint) Random.Shared.Next(1 << 14, 2 << 14);
                }
                while (UsedUid.Contains(tamer.DigimonUID))
                {
                    tamer.DigimonUID = (uint) Random.Shared.Next(1 << 14, 2 << 14);
                }
                tamer.DigimonList[0].UID = tamer.DigimonUID;
                Enter(tamer);
                var charInfo = new CharInfo(tamer);
                tamer.Client?.Send(charInfo.ToArray());
            }
        }
        
        lock (Tamers)
        {
            
            
            
        }
        //Update func for tamers, digimons and monsters
        Thread.Sleep(1); //1 ms
    }
}

public void Spawn(Character owner)
{
    foreach (var (name, tamer) in Tamers)
    {
        if (name == owner.Name) continue;
        var writer = new PacketWriter();
        writer.Type(9905);
        writer.WriteUInt(tamer.UID);
        writer.WriteUInt(tamer.Partner.UID);
        writer.WriteShort(1493);
        writer.WriteShort(1493);
        writer.WriteInt(0);
        writer.WriteInt(0);
        var writer2 = new PacketWriter();
        writer2.Type(9905);
        writer2.WriteUInt(tamer.UID);
        writer2.WriteUInt(tamer.Partner.UID);
        writer2.WriteShort(1493);
        writer2.WriteShort(1493);
        writer2.WriteInt(0);
        writer2.WriteInt(0);
        tamer.Client?.Send(new SpawnPlayer(owner, owner.UID).ToArray());
        tamer.Client?.Send(writer);
        owner.Client?.Send(new SpawnPlayer(tamer, tamer.UID).ToArray());
        owner.Client?.Send(writer2);
    }
}
public void KillMonster(int handle)
{
    var entity = Monsters.FirstOrDefault(monster => monster.Handle == handle);
    if (entity != null)
    {
        entity.HP = 0;
    }
}

public int ReduceMonsterHealth(int handle, int damage)
{
    var entity = Monsters.FirstOrDefault(monster => monster.Handle == handle);
    if (entity != null)
    {
        var monsterNewHealth = entity.HP - damage;
        if (monsterNewHealth < 0)
        {
            KillMonster(handle);
        }
        else
        {
            entity.HP = monsterNewHealth;
            return entity.HP;
        }
    }

    return 0;
}

public void Send(byte[] buffer, uint sender = 0)
{
    foreach (var (name, tamer) in Tamers)
    {
        if(sender == tamer.UID) continue;
        tamer.Client?.Send(buffer);
    }
}
}
