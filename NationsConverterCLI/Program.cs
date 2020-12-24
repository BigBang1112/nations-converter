using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter.Stages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GBX.NET.BlockInfo;
using System.Reflection;
using NationsConverter;

namespace NationsConverterCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = args.FirstOrDefault();

            if (fileName == null)
            {
                Console.WriteLine("Please input a map file to the program.\n");
                Console.Write("Press any key to continue...");
                Console.ReadKey();

                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var localDirectory = Path.GetDirectoryName(assembly.Location);
            var outputDirectory = localDirectory;
            if (args.Length >= 2)
                outputDirectory = args[1];

            Log.OnLogEvent += Log_LoggedMainEvent;

            BlockInfoManager.BlockModels
                = JsonConvert.DeserializeObject<Dictionary<string, BlockModel>>(
                    File.ReadAllText("StadiumBlockModels.json")
                );

            List<string> files = new List<string>
            {
                fileName
            };

            var maps = new List<GameBox<CGameCtnChallenge>>();

            var sheet = YamlManager.Parse<Sheet>(localDirectory + "/Sheets/Stock.yml");
            var sheets = new Sheet[]
            {
                YamlManager.Parse<Sheet>(localDirectory + "/Sheets/Custom.yml")
            };

            var sheetMgr = new SheetManager(sheet, sheets);
            sheetMgr.UpdateDefinitions();

            var converter = new Converter()
            {
                Parameters = new ConverterParameters
                {
                    Definitions = sheetMgr.Definitions
                }
            };

            // Preparation for conversion

            foreach (var f in files) // Maps to add for conversion
            {
                var mapGbx = GameBox.Parse<CGameCtnChallenge>(f);
                var map = mapGbx.MainNode;

                converter.EmbedManager.CopyUsedEmbed(map, sheetMgr.Definitions, converter.Parameters);

                maps.Add(mapGbx);
            }

            // Actual conversion

            foreach (var gbxMap in maps)
            {
                var map = gbxMap.MainNode;

                var chunk01F = map.GetChunk<CGameCtnChallenge.Chunk0304301F>();

                int version;
                if (chunk01F.Version <= 1)
                    version = GameVersion.TMUF;
                else
                    version = GameVersion.TM2;

                converter.Convert(map, version);

                gbxMap.Save($"{outputDirectory}/{Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(gbxMap.FileName))}.Map.Gbx");
            }
        }

        static void Log_LoggedMainEvent(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
