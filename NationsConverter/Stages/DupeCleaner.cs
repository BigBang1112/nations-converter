using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter.Stages
{
    public class DupeCleaner : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            map.Blocks.RemoveAll(x =>
            {
                if (x.Name == "PlatformTechBase")
                    foreach (var block in map.Blocks)
                        if (block.Coord == x.Coord && block.Name == "DecoWallBasePillar")
                            return true;

                return false;
            });
        }
    }
}
