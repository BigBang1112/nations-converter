using GBX.NET;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter
{
    public class ConverterParameters
    {
        public Int3 StadiumOffsetCoord { get; set; }
        public Vec3 StadiumOffset { get; set; }
        public Int3 Stadium2RelativeOffsetCoord { get; set; }
        public Vec3 Stadium2RelativeOffset { get; set; }

        public Definitions Definitions { get; set; }
        public Dictionary<string, ItemSkinPack[]> ItemSkinPacks { get; set; }
        public bool IgnoreMediaTracker { get; set; }
        public bool ChristmasMode { get; set; }
        public bool ClassicMod { get; set; }

        public ConverterParameters()
        {
            StadiumOffsetCoord = (8, 0, 8);
            StadiumOffset = StadiumOffsetCoord * 32f;
            Stadium2RelativeOffsetCoord = (0, 1, 0);
            Stadium2RelativeOffset = Stadium2RelativeOffsetCoord * (32, 8, 32);
        }
    }
}
