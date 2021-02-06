using GBX.NET;
using GBX.NET.Engines.Game;
using System;
using System.Linq;

namespace NationsConverter.Stages
{
    /// <summary>
    /// Converts the decoration, increases the map size to 48x48.
    /// </summary>
    public class BaseConverter : IStage
    {
        string[] supportedDecoration = new string[] { "Sunrise", "Day", "Sunset", "Night" };

        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            if (!supportedDecoration.Contains(map.Decoration.ID))
                throw new NotSupportedException("Decorations other than default aren't supported.");
            map.Decoration.ID = "48x48" + map.Decoration.ID;
            map.Collection = new Collection(26); // 26 is TM® Stadium env
            map.Size = (48, 40, 48);

            if (parameters.ClassicMod)
                map.ModPackDesc = new FileRef(3, Convert.FromBase64String("4a/2cmSyYjq4kgH6L3ujXSNvOBQTD9qzHabu1Ebpz8Y="),
                    "Skins\\Stadium\\Mod\\ClassicStadium.zip",
                    "http://maniacdn.net/adamkooo/ClassicStadium.zip");

            if (version >= GameVersion.TM2)
                map.ThumbnailPosition += (8, 0, 8) * map.Collection.GetBlockSize();
        }
    }
}
