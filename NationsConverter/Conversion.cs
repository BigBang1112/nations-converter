using GBX.NET;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter
{
    public class Conversion
    {
        public ConversionBlock Block { get; set; }
        public ConversionBlock[] Blocks { get; set; }
        public ConversionItem Item { get; set; }
        public ConversionItem[] Items { get; set; }
        public ConversionFreeBlock FreeBlock { get; set; }
        public ConversionFreeBlock[] FreeBlocks { get; set; }
        public int[] Size { get; set; }
        public float[] Center { get; set; }
        public int OffsetDir { get; set; }
        public int OffsetY { get; set; }
        public string Macroblock { get; set; }

        public Conversion Air { get; set; }
        public Conversion Ground { get; set; }
        public Conversion DirtGround { get; set; }
        public Conversion GrassGround { get; set; }
        public Conversion FabricGround { get; set; }

        public bool RemoveGround { get; set; }
        public bool OffsetPivotByBlockModel { get; set; }

        public bool MakeFabric { get; set; }
        public bool MakeFabricOnGround { get; set; }

        public static implicit operator Conversion(string blockName) => new Conversion() { Block = new ConversionBlock() { Name = blockName } };
    }
}
