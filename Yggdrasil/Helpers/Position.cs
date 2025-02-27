﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yggdrasil.Database;

namespace Yggdrasil.Helpers
{
    public class Position
    {
        public int Map = 1;
        public int PosX = 0;
        public int PosY = 0;

        public Position()
        {
            Map = 3; //NEW DATS! -> 1 -> DATS_001
            PosX = 50;
            PosY = 50;
        }

        public Position(int map, int x, int y)
        {
            Map = map;
            PosX = x;
            PosY = y;
        }

        public Position(Portal Portal)
        {
            this.Map = Portal.MapId;
            this.PosX = Portal.uInts2[0];
            this.PosY = Portal.uInts2[1];
        }

        public override string ToString()
        {
            
            return string.Format("{0} {3} [{1}, {2}]", MapData.DisplayName, PosX, PosY, Map);
        }

        public MapData MapData
        {
            get
            {
                return MapListDB.GetMap(Map);
            }
        }

        public string MapName
        {
            get
            {
                return MapData.DisplayName;
            }
        }

        public Position Clone()
        {
            return new Position(Map, PosX, PosY);
        }
    }
}
