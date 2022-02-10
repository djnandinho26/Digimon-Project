using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yggdrasil.Helpers;
using System.IO;
using Yggdrasil;
using Yggdrasil.Entities;

namespace Yggdrasil.Database
{

    public class MonstersDB
    {
        public static Dictionary<int, Monsters> monsters = new Dictionary<int, Monsters>();

        public static void Load(string fileName, List<MonsterEntity> MonsterEntity)
        {
            using (Stream s = File.OpenRead(fileName))
            {
                if (monsters.Count > 0) return;
                using (BitReader read = new BitReader(s))
                {
                    int count = read.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        read.Seek(4 + i * 396);
                        Monsters monster = new Monsters();
                        monster.MonsterID = read.ReadInt();
                        ; int Model = read.ReadInt();
                        monster.Name = read.ReadZString(Encoding.Unicode, 32);
                        //Console.WriteLine(monster.Name);
                        //SysCons.LogInfo($"MonsterId: {monster.MonsterID} | MonsterName: {monster.Name}");
                        read.Seek(4 + 332 + i * 396);
                        monster.Level = read.ReadShort();

                        for (int p = 0; p < MonsterEntity.Count; p++)
                        {
                            if (MonsterEntity[p].Species == monster.MonsterID)
                            {
                                MonsterEntity[p].Level = monster.Level;
                            }
                        }

                        monsters.Add(monster.MonsterID, monster);
                    }
                }
            }

            SysCons.LogDB("Monsters.bin", "Loaded {0} Monsters", monsters.Count);
        }

        public static Monsters GetMonster(int MonsterID)
        {
            Monsters iData = null;
            foreach (KeyValuePair<int, Monsters> kvp in monsters)
            {
                if (kvp.Value.MonsterID == MonsterID)
                {
                    iData = kvp.Value;
                    break;
                }
            }
            return iData;
        }
    }

    public class Monsters
    {
        public short Level;
        public string Name;
        public string Tag;
        public int MonsterID;
    }


}