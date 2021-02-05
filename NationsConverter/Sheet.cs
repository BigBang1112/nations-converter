using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter
{
    public class Sheet
    {
        public int Version { get; set; }
        public string Name { get; set; }
        public bool RemoveGround { get; set; }
        public Definitions Blocks { get; set; }
        public Dictionary<string, ItemSkinPack[]> ItemSkinPacks { get; set; } = new Dictionary<string, ItemSkinPack[]>();
    }
}
