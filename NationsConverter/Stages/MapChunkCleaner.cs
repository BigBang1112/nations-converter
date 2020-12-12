using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter.Stages
{
    public class MapChunkCleaner : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            map.RemoveChunk<CGameCtnChallenge.Chunk03043019>();
            map.RemoveChunk<CGameCtnChallenge.Chunk03043029>();

            if (version >= GameVersion.TM2)
            {
                map.RemoveChunk(0x03043034);
                //map.RemoveChunk<CGameCtnChallenge.Chunk03043036>();
                map.RemoveChunk(0x03043038);
                map.RemoveChunk<CGameCtnChallenge.Chunk0304303D>();
                map.RemoveChunk(0x0304303E);
                //map.RemoveChunk<CGameCtnChallenge.Chunk03043040>();
                map.RemoveChunk<CGameCtnChallenge.Chunk03043042>();
                map.RemoveChunk<CGameCtnChallenge.Chunk03043043>();
                map.RemoveChunk<CGameCtnChallenge.Chunk03043044>();
                map.RemoveChunk<CGameCtnChallenge.Chunk03043048>();
                map.RemoveChunk<CGameCtnChallenge.Chunk0304304B>();
                map.RemoveChunk(0x0304304D);
                map.RemoveChunk(0x0304304F);
                map.RemoveChunk(0x03043050);
                map.RemoveChunk<CGameCtnChallenge.Chunk03043051>();
                map.RemoveChunk(0x03043052);
                map.RemoveChunk(0x03043053);
                map.RemoveChunk(0x03043055);
                map.RemoveChunk(0x03043056);
            }
        }
    }
}
