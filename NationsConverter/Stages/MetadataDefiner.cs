using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Configuration;
using System.Linq;

namespace NationsConverter.Stages
{
    public class MetadataDefiner : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            var assembly = Assembly.GetEntryAssembly();
            var assemblyGBXNET = assembly.GetReferencedAssemblies().FirstOrDefault(x => x.Name == "GBX.NET");

            map.CreateChunk<CGameCtnChallenge.Chunk03043044>();
            map.ScriptMetadata.Declare("MadeWithNationsConverter", true);
            map.ScriptMetadata.Declare("NC_Assembly", assembly.FullName);
            map.ScriptMetadata.Declare("NC_GBXNET_Assembly", assemblyGBXNET.FullName);
            map.ScriptMetadata.Declare("NC_EarlyAccess", false);
        }
    }
}
