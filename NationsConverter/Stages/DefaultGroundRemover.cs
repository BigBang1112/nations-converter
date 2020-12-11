﻿using GBX.NET;
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
            map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Blocks/NationsConverter/NoGround.Block.Gbx", "Blocks/NationsConverter");

            for (var x = 0-1; x < 32+1; x++)
            {
                for (var z = 0-1; z < 32+1; z++)
                {
                    Int3 coord = (x + 8, 0, z + 8);
                    if (version <= GameVersion.TMUF)
                        coord += parameters.Stadium2RelativeOffsetCoord.XZ;
                    map.Blocks.Add(new CGameCtnBlock(@"NationsConverter\NoGround.Block.Gbx_CustomBlock", Direction.North, coord) { IsGround = true });
                }
            }
        }
    }
}
