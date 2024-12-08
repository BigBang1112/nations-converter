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
        // Update to modern data chunks in cases where it crashes
        mapOut.ClipIntro = TransferMediaTracker(mapIn.ClipIntro);
        mapOut.ClipGroupInGame = TransferMediaTracker(mapIn.ClipGroupInGame);
        mapOut.ClipGroupEndRace = TransferMediaTracker(mapIn.ClipGroupEndRace);
        mapOut.ClipAmbiance = TransferMediaTracker(mapIn.ClipAmbiance);
    }

    private CGameCtnMediaClip? TransferMediaTracker(CGameCtnMediaClip? clip)
    {
        if (clip is null)
        {
            return null;
        }

        var newClip = new CGameCtnMediaClip
        {
            Tracks = new List<CGameCtnMediaTrack>(),
            LocalPlayerClipEntIndex = clip.LocalPlayerClipEntIndex,
            StopWhenLeave = clip.StopWhenLeave,
            StopWhenRespawn = clip.StopWhenRespawn,
            Name = clip.Name,
        };
        foreach (var chunk in clip.Chunks) newClip.Chunks.Add(chunk);

        foreach (var track in clip.Tracks)
        {
            var newTrack = new CGameCtnMediaTrack
            {
                Blocks = new List<CGameCtnMediaBlock>(),
                Name = track.Name,
                IsCycling = track.IsCycling,
                IsKeepPlaying = track.IsKeepPlaying,
                IsReadOnly = track.IsReadOnly,
            };
            foreach (var chunk in track.Chunks) newTrack.Chunks.Add(chunk);

            newClip.Tracks.Add(newTrack);

            foreach (var block in track.Blocks)
            {
                switch (block)
                {
                    case CGameCtnMediaBlockCameraCustom { Keys: not null } cameraCustom:
                        var newCameraCustom = new CGameCtnMediaBlockCameraCustom
                        {
                            Keys = new List<CGameCtnMediaBlockCameraCustom.Key>()
                        };
                        foreach (var chunk in cameraCustom.Chunks) newCameraCustom.Chunks.Add(chunk);

                        newTrack.Blocks.Add(newCameraCustom);

                        foreach (var key in cameraCustom.Keys)
                        {
                            newCameraCustom.Keys.Add(new CGameCtnMediaBlockCameraCustom.Key
                            {
                                Time = key.Time,
                                Position = key.Position + centerOffset * blockSize,
                                Anchor = key.Anchor,
                                AnchorRot = key.AnchorRot,
                                AnchorVis = key.AnchorVis,
                                Fov = key.Fov,
                                Interpolation = key.Interpolation,
                                LeftTangent = key.LeftTangent,
                                NearZ = key.NearZ,
                                PitchYawRoll = key.PitchYawRoll,
                                RightTangent = key.RightTangent,
                                Target = key.Target,
                                TargetPosition = key.TargetPosition,
                                U01 = key.U01,
                                U02 = key.U02,
                                U03 = key.U03,
                                U04 = key.U04,
                                U05 = key.U05,
                                U06 = key.U06,
                                U07 = key.U07,
                                U08 = key.U08,
                                U09 = key.U09,
                            });
                        }
                        break;
                    case CGameCtnMediaBlockCameraPath { Keys: not null } cameraPath:
                        var newCameraPath = new CGameCtnMediaBlockCameraPath
                        {
                            Keys = new List<CGameCtnMediaBlockCameraPath.Key>()
                        };
                        foreach (var chunk in cameraPath.Chunks) newCameraPath.Chunks.Add(chunk);

                        newTrack.Blocks.Add(newCameraPath);

                        foreach (var key in cameraPath.Keys)
                        {
                            newCameraPath.Keys.Add(new CGameCtnMediaBlockCameraPath.Key
                            {
                                Time = key.Time,
                                Position = key.Position + centerOffset * blockSize,
                                Anchor = key.Anchor,
                                AnchorRot = key.AnchorRot,
                                AnchorVis = key.AnchorVis,
                                Fov = key.Fov,
                                NearZ = key.NearZ,
                                PitchYawRoll = key.PitchYawRoll,
                                Target = key.Target,
                                TargetPosition = key.TargetPosition,
                                U01 = key.U01,
                                U02 = key.U02,
                                U03 = key.U03,
                                Weight = key.Weight,
                            });
                        }
                        break;
                    case CGameCtnMediaBlockFxBloom:
                        // TODO: convert to CGameCtnMediaBlockBloomHdr effectively
                        break;
                    default:
                        newTrack.Blocks.Add(block);
                        break;
                }
            }
        }

        return newClip;
    }

    private CGameCtnMediaClipGroup? TransferMediaTracker(CGameCtnMediaClipGroup? clipGroup)
    {
        if (clipGroup is null)
        {
            return null;
        }

        var newClipGroup = new CGameCtnMediaClipGroup { Clips = new List<CGameCtnMediaClipGroup.ClipTrigger>() };
        foreach (var chunk in clipGroup.Chunks) newClipGroup.Chunks.Add(chunk);

        foreach (var (clip, trigger) in clipGroup.Clips)
        {
            var newClip = TransferMediaTracker(clip);
            if (newClip is not null)
            {
                newClipGroup.Clips.Add(new(newClip, trigger));
            }
        }

        return newClipGroup;
    }
}
