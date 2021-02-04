using GBX.NET.Engines.Game;

namespace NationsConverter.Stages
{
    public class ModeConverter : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            map.ChallengeParameters.MapType = "TrackMania\\TM_Race";

            map.ChallengeParameters.RemoveChunk<CGameCtnChallengeParameters.Chunk0305B006>(); // Causes "Couldn't load map" on ESWC

            if (version < GameVersion.TM2)
                map.ChallengeParameters.CreateChunk<CGameCtnChallengeParameters.Chunk0305B00E>();

            map.Kind = CGameCtnChallenge.MapKind.InProgress;
        }
    }
}
