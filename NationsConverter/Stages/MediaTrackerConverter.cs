using GBX.NET;
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
                offsetCoord -= (0, 8, 0);

                map.ClipIntro?.Tracks?.ForEach(y => y.Blocks.RemoveAll(z => z is CGameCtnMediaBlockCameraGame));
                Array.ForEach(map.ClipGroupInGame?.Clips ?? new CGameCtnMediaClip[0], x => x.Tracks.ForEach(y => y.Blocks.RemoveAll(z => z is CGameCtnMediaBlockCameraGame)));
                Array.ForEach(map.ClipGroupEndRace?.Clips ?? new CGameCtnMediaClip[0], x => x.Tracks.ForEach(y => y.Blocks.RemoveAll(z => z is CGameCtnMediaBlockCameraGame)));
                map.ClipAmbiance?.Tracks?.ForEach(y => y.Blocks.RemoveAll(z => z is CGameCtnMediaBlockCameraGame));
                // TODO: fix the 0x007 chunk of CameraGame in GBX.NET
            }

            map.TransferMediaTrackerTo049();
            map.OffsetMediaTrackerCameras(offset);
            map.OffsetMediaTrackerTriggers(offsetCoord);
        }
    }
}
