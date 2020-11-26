using GBX.NET;
using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter.Stages
{
    public class DefaultGroundRemover : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            map.ImportFileToEmbed("UserData/Blocks/NationsConverter/NoGround.Block.Gbx", "Blocks/NationsConverter");

            for (var x = 1; x <= map.Size.GetValueOrDefault().X; x++)
            {
                for (var z = 1; z <= map.Size.GetValueOrDefault().Z; z++)
                {
                    Int3 coord = (x, 0, z);
                    if (version >= GameVersion.TM2)
                        coord += parameters.Stadium2RelativeOffsetCoord.XZ;
                    map.Blocks.Add(new CGameCtnBlock(@"NationsConverter\NoGround.Block.Gbx_CustomBlock", Direction.North, coord) { IsGround = true });
                }
            }
        }
    }
}
