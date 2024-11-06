using GBX.NET.Engines.Game;

namespace NationsConverter.Stages;

internal sealed class MediaTrackerStage
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly NationsConverterConfig config;

    public MediaTrackerStage(CGameCtnChallenge mapIn, CGameCtnChallenge mapOut, NationsConverterConfig config)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.config = config;
    }

    public void Convert()
    {
        if (!config.IncludeMediaTracker)
        {
            return;
        }

        // TODO
        // Align cameras
        // Update to modern data chunks in cases where it crashes

        mapOut.ClipIntro = mapIn.ClipIntro;
        mapOut.ClipGroupInGame = mapIn.ClipGroupInGame;
        mapOut.ClipGroupEndRace = mapIn.ClipGroupEndRace;
        mapOut.ClipAmbiance = mapIn.ClipAmbiance;
    }
}
