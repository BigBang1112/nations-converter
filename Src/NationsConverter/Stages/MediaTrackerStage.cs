﻿using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using NationsConverterShared.Models;

namespace NationsConverter.Stages;

internal sealed class MediaTrackerStage : BlockStageBase
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    public MediaTrackerStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        NationsConverterConfig config,
        ManualConversionSetModel conversionSet,
        ILogger logger) : base(mapIn, mapOut, conversionSet, logger)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.config = config;
        this.logger = logger;
    }

    public override void Convert()
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

        mapOut.ClipTriggerSize = (1, 1, 1);

        base.Convert();
    }

    protected override void ConvertBlock(CGameCtnBlock block, ManualConversionModel conversion)
    {
        if (mapOut.ClipGroupInGame is null)
        {
            return;
        }

        if (conversion.Waypoint is not WaypointType.Start and not WaypointType.StartFinish)
        {
            return;
        }

        var expectedCoord = block.Coord * (BlockSize.X / 32, BlockSize.Y / 8, BlockSize.Z / 32) + (0, 8, 0) + CenterOffset;

        logger.LogInformation("Checking if there's trigger at {Coord} due to possible camera softlock...", expectedCoord);

        foreach (var clip in mapOut.ClipGroupInGame.Clips)
        {
            if (clip.Trigger.Coords?.Contains(expectedCoord) == true)
            {
                // Trigger already exists on this position
                logger.LogInformation("Trigger already exists at {Coord}, skipping the prevention.", expectedCoord);
                return;
            }
        }

        logger.LogInformation("Adding trigger at {Coord} to prevent camera softlock...", expectedCoord);

        var emptyClip = new CGameCtnMediaClip
        {
            Name = "NADEO TRIGGER BUG FIX"
        };
        emptyClip.CreateChunk<CGameCtnMediaClip.Chunk0307900D>();

        mapOut.ClipGroupInGame.Clips.Add(new(emptyClip, new()
        {
            Coords = [expectedCoord],
            Condition = 0,
            ConditionValue = 0,
            U01 = 0,
            U02 = 0,
            U03 = 0,
            U04 = 0,
        }));
    }

    private CGameCtnMediaClip? TransferMediaTracker(CGameCtnMediaClip? clip)
    {
        if (clip is null)
        {
            return null;
        }

        var newClip = new CGameCtnMediaClip
        {
            Tracks = [],
            LocalPlayerClipEntIndex = clip.LocalPlayerClipEntIndex,
            StopWhenLeave = clip.StopWhenLeave,
            StopWhenRespawn = clip.StopWhenRespawn,
            Name = clip.Name,
        };
        newClip.CreateChunk<CGameCtnMediaClip.Chunk0307900D>();

        foreach (var track in clip.Tracks)
        {
            var newTrack = new CGameCtnMediaTrack
            {
                Blocks = [],
                Name = track.Name,
                IsCycling = track.IsCycling,
                IsKeepPlaying = track.IsKeepPlaying,
                IsReadOnly = track.IsReadOnly,
            };
            newTrack.CreateChunk<CGameCtnMediaTrack.Chunk03078001>();
            newTrack.CreateChunk<CGameCtnMediaTrack.Chunk03078005>();

            newClip.Tracks.Add(newTrack);

            foreach (var block in track.Blocks)
            {
                switch (block)
                {
                    case CGameCtnMediaBlockCameraCustom { Keys: not null } cameraCustom:
                        var newCameraCustom = new CGameCtnMediaBlockCameraCustom
                        {
                            Keys = []
                        };
                        newCameraCustom.CreateChunk<CGameCtnMediaBlockCameraCustom.Chunk030A2006>();

                        newTrack.Blocks.Add(newCameraCustom);

                        foreach (var key in cameraCustom.Keys)
                        {
                            newCameraCustom.Keys.Add(new CGameCtnMediaBlockCameraCustom.Key
                            {
                                Time = key.Time,
                                Position = key.Position + CenterOffset * BlockSize,
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
                            Keys = []
                        };
                        foreach (var chunk in cameraPath.Chunks) newCameraPath.Chunks.Add(chunk);

                        newTrack.Blocks.Add(newCameraPath);

                        foreach (var key in cameraPath.Keys)
                        {
                            newCameraPath.Keys.Add(new CGameCtnMediaBlockCameraPath.Key
                            {
                                Time = key.Time,
                                Position = key.Position + CenterOffset * BlockSize,
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
                    case CGameCtnMediaBlockGhost:
                        // TODO: do wild ghost conversion
                        break;
                    case CGameCtnMediaBlockCameraGame cameraGame:
                        var gameCam = cameraGame.GameCam;

                        if (!string.IsNullOrEmpty(cameraGame.GameCamId))
                        {
                            gameCam = cameraGame.GameCamId switch
                            {
                                "Internal" => CGameCtnMediaBlockCameraGame.EGameCam.Internal,
                                "Behind" => CGameCtnMediaBlockCameraGame.EGameCam.External,
                                _ => CGameCtnMediaBlockCameraGame.EGameCam.Default,
                            };
                        }
                        else if (cameraGame.GameCamOld != 0)
                        {
                            gameCam = cameraGame.GameCamOld switch
                            {
                                CGameCtnMediaBlockCameraGame.EGameCamOld.Internal => CGameCtnMediaBlockCameraGame.EGameCam.Internal,
                                _ => CGameCtnMediaBlockCameraGame.EGameCam.Default,
                            };
                        }

                        var newCameraGame = new CGameCtnMediaBlockCameraGame
                        {
                            CamFarClipPlane = cameraGame.CamFarClipPlane,
                            CamNearClipPlane = cameraGame.CamNearClipPlane,
                            CamFov = cameraGame.CamFov,
                            CamPitchYawRoll = cameraGame.CamPitchYawRoll,
                            CamPosition = cameraGame.CamPosition,
                            ClipEntId = cameraGame.ClipEntId,
                            GameCam = gameCam,
                            Start = cameraGame.Start,
                            End = cameraGame.End,
                        };
                        newCameraGame.CreateChunk<CGameCtnMediaBlockCameraGame.Chunk03084007>().Version = 4;

                        newTrack.Blocks.Add(newCameraGame);
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

        var newClipGroup = new CGameCtnMediaClipGroup { Clips = [] };
        newClipGroup.CreateChunk<CGameCtnMediaClipGroup.Chunk0307A003>(); // dunno why that became requirement?

        foreach (var (clip, trigger) in clipGroup.Clips)
        {
            var newClip = TransferMediaTracker(clip);
            if (newClip is not null)
            {
                var coordEnumerable = trigger.Coords?.Select(coord => coord with
                {
                    X = coord.X * BlockSize.X / 32,
                    Y = coord.Y * BlockSize.Y / 8 + 8,
                    Z = coord.Z * BlockSize.Z / 32,
                } + CenterOffset);
                var coords = new List<Int3>();

                foreach (var coord in coordEnumerable ?? [])
                {
                    for (var i = 0; i < BlockSize.Y / 8; i++)
                    {
                        coords.Add(coord with { Y = coord.Y + i });
                    }
                }

                var newTrigger = new CGameCtnMediaClipGroup.Trigger
                {
                    Coords = coords,
                    Condition = trigger.Condition,
                    ConditionValue = trigger.ConditionValue,
                    U01 = trigger.U01,
                    U02 = trigger.U02,
                    U03 = trigger.U03,
                    U04 = trigger.U04,
                };

                newClipGroup.Clips.Add(new(newClip, newTrigger));
            }
        }

        return newClipGroup;
    }
}
