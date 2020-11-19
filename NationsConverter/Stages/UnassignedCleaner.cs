using GBX.NET.Engines.Game;
using System.Linq;

namespace NationsConverter.Stages
{
    public class UnassignedCleaner : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            map.Blocks = map.Blocks.Where(x => x.Name != "Unassigned1").ToList(); // Copy of stadium blocks
            // Flags aren't -1 anymore, probably GBX.NET bug
        }
    }
}
