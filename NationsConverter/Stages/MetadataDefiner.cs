using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Configuration;

namespace NationsConverter.Stages
{
    public class MetadataDefiner : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            var assembly = Assembly.GetEntryAssembly();

            map.CreateChunk<CGameCtnChallenge.Chunk03043044>();
            map.ScriptMetadata.Declare("MadeWithNationsConverter", true);
            map.ScriptMetadata.Declare("NC_Assembly", assembly.FullName);
        }
    }
}
