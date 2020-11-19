using GBX.NET;
using GBX.NET.Engines.Game;

namespace NationsConverter.Stages
{
    /// <summary>
    /// Converts the decoration, increases the map size to 48x48.
    /// </summary>
    public class BaseConverter : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            map.Decoration.ID = "48x48" + map.Decoration.ID;
            map.Collection = new Collection(26); // 26 is TM® Stadium env
            map.Size = (48, 40, 48);
        }
    }
}
