namespace NationsConverter
{
    public class ConversionBlock
    {
        public string Name { get; set; }
        public int OffsetY { get; set; }
        public int OffsetDir { get; set; }
        public bool? Ghost { get; set; }
        public int[] Size { get; set; }
        public int? Flags { get; set; }
        public bool Custom { get; set; }

        public int[] OffsetCoord { get; set; } = new int[] { 0, 0, 0 };
        public int[] OffsetCoord2 { get; set; }
        public int[][] OffsetCoords { get; set; }
        public bool IgnoreGround { get; set; }
        public byte? Variant { get; set; }
        public float[] CenterFromCoord { get; set; }
        public bool? Bit17 { get; set; }
        public bool? Bit21 { get; set; }

        public static implicit operator ConversionBlock(string name) => new ConversionBlock() { Name = name };
    }
}
