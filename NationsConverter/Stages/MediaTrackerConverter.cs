using GBX.NET;
using GBX.NET.Engines.Game;
using System;
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

            //if (version >= GameVersion.TM2)
            //    offset -= MainConverter.Parameters.Stadium2RelativeOffset;

            map.TransferMediaTrackerTo049();
            map.OffsetMediaTrackerCameras(offset);
            map.OffsetMediaTrackerTriggers(offsetCoord * (3, 1, 3));
        }
    }
}
