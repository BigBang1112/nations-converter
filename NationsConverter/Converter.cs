using GBX.NET.Engines.Game;
using NationsConverter.Stages;

namespace NationsConverter
{
    public class Converter
    {
        public ConverterParameters Parameters { get; set; } = new ConverterParameters();
        public EmbedManager EmbedManager { get; set; } = new EmbedManager();

        public UnassignedCleaner UnassignedCleaner { get; } = new UnassignedCleaner();
        public GroundPlacer GroundMaker { get; } = new GroundPlacer();
        public BaseConverter BaseConverter { get; } = new BaseConverter();
        public ModeConverter ModeConverter { get; } = new ModeConverter();
        public MediaTrackerConverter MediaTrackerConverter { get; } = new MediaTrackerConverter();
        public BlockConverter BlockConverter { get; } = new BlockConverter();
        public MapChunkCleaner MapChunkCleaner { get; } = new MapChunkCleaner();
        public DupeCleaner DupeCleaner { get; } = new DupeCleaner();
        public DefaultGroundRemover DefaultGroundRemover { get; } = new DefaultGroundRemover();
        public MetadataDefiner MetadataDefiner { get; } = new MetadataDefiner();

        public void Convert(CGameCtnChallenge map, int version)
        {
            UnassignedCleaner.Process(map, version, Parameters);
            BaseConverter.Process(map, version, Parameters);
            GroundMaker.Process(map, version, Parameters);
            ModeConverter.Process(map, version, Parameters);
            MediaTrackerConverter.Process(map, version, Parameters);
            BlockConverter.Process(map, version, Parameters);
            DupeCleaner.Process(map, version, Parameters);
            MapChunkCleaner.Process(map, version, Parameters);
            DefaultGroundRemover.Process(map, version, Parameters);
            MetadataDefiner.Process(map, version, Parameters);
        }
    }
}
