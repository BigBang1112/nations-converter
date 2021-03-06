﻿using GBX.NET;
using GBX.NET.Engines.Game;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter.Stages
{
    public class MediaTrackerConverter : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            var offset = parameters.StadiumOffset;
            var offsetCoord = parameters.StadiumOffsetCoord;

            if (version <= GameVersion.TMUF)
            {
                offsetCoord *= (3, 1, 3);
                //offset -= MainConverter.Parameters.Stadium2RelativeOffset;
            }

            if(version >= GameVersion.TM2)
            {
                offsetCoord += (0, -8, 0);

                if (map.GetChunk<CGameCtnChallenge.Chunk03043049>()?.TriggerSize == (3, 1, 3))
                    offsetCoord += (16, 0, 16);
            }

            map.TransferMediaTrackerTo049();
            map.OffsetMediaTrackerCameras(offset);
            map.OffsetMediaTrackerTriggers(offsetCoord);

            if (parameters.IgnoreMediaTracker)
                map.RemoveChunk<CGameCtnChallenge.Chunk03043049>();
        }
    }
}
