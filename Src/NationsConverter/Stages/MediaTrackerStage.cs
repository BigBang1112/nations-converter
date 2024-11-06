using GBX.NET;
using GBX.NET.Engines.Game;

namespace NationsConverter.Stages;

internal sealed class MediaTrackerStage
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly NationsConverterConfig config;

    private readonly Int3 blockSize;
    private readonly Int3 centerOffset;

    public MediaTrackerStage(CGameCtnChallenge mapIn, CGameCtnChallenge mapOut, NationsConverterConfig config)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.config = config;

        blockSize = mapIn.Collection.GetValueOrDefault().GetBlockSize();
        centerOffset = new Int3((mapOut.Size.X - mapIn.Size.X) / 2, 0, (mapOut.Size.Z - mapIn.Size.Z) / 2) ;
    }

    public void Convert()
    {
        if (!config.IncludeMediaTracker)
        {
            return;
        }

        // TODO
        // !!! Recreate media tracker from scratch, as this way its mutating
        // Update to modern data chunks in cases where it crashes
        OffsetMediaTracker(mapIn.ClipIntro);
        OffsetMediaTracker(mapIn.ClipGroupInGame);
        OffsetMediaTracker(mapIn.ClipGroupEndRace);
        OffsetMediaTracker(mapIn.ClipAmbiance);


        mapOut.ClipIntro = mapIn.ClipIntro;
        mapOut.ClipGroupInGame = mapIn.ClipGroupInGame;
        mapOut.ClipGroupEndRace = mapIn.ClipGroupEndRace;
        mapOut.ClipAmbiance = mapIn.ClipAmbiance;
    }

    private void OffsetMediaTracker(CGameCtnMediaClip? clip)
    {
        if (clip is null)
        {
            return;
        }

        foreach (var track in clip.Tracks)
        {
            foreach (var block in track.Blocks)
            {
                if (block is CGameCtnMediaBlockCameraCustom { Keys: not null } cameraCustom)
                {
                    foreach (var key in cameraCustom.Keys)
                    {
                        key.Position += centerOffset * blockSize;
                    }
                    continue;
                }

                if (block is CGameCtnMediaBlockCameraPath { Keys: not null } cameraPath)
                {
                    foreach (var key in cameraPath.Keys)
                    {
                        key.Position += centerOffset * blockSize;
                    }
                    continue;
                }
            }
        }
    }

    private void OffsetMediaTracker(CGameCtnMediaClipGroup? clipGroup)
    {
        if (clipGroup is null)
        {
            return;
        }

        foreach (var (clip, _) in clipGroup.Clips)
        {
            OffsetMediaTracker(clip);
        }
    }
}
