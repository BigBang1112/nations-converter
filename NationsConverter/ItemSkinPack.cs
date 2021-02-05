using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter
{
    public class ItemSkinPack
    {
        public ConversionItem[] Items { get; set; } = new ConversionItem[] { new ConversionItem() };
        public Dictionary<string, string> Skins { get; set; }
    }
}
