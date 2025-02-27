﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yggdrasil.Helpers;
using System.IO;
using Yggdrasil.Entities;
using Yggdrasil;

namespace Yggdrasil.Database
{
    public class SvLineUpDB
    {
        public static Dictionary<int, WorldMapInfo> WorldMapInfoList = new Dictionary<int, WorldMapInfo>();

        public static void Load(string fileName)
        {
            if (WorldMapInfoList.Count > 0) return;
            using (Stream s = File.OpenRead(fileName))
            {
                using (BitReader read = new BitReader(s))
                {
                    int count = read.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        read.Seek(4 + i * 476);
                        WorldMapInfo mapInfo = new WorldMapInfo();
                        mapInfo.s_nID = read.ReadUShort();
                        mapInfo.s_nWorldType = read.ReadByte();
                        mapInfo.s_nUI_X = read.ReadUShort();
                        mapInfo.s_nUI_Y = read.ReadUShort();
                        if (!WorldMapInfoList.ContainsKey(mapInfo.s_nID))
                        {
                            WorldMapInfoList.Add(mapInfo.s_nID, mapInfo);
                        }

                    }
                }
            }
            SysCons.LogDB("SvLineUp.bin", "Loaded {0} SvLineUp.", WorldMapInfoList.Count);
        }
    }
}

