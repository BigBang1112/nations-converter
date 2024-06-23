using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Game;
using GBX.NET.Hashing;
using GBX.NET.LZO;
using NationsConverter;

Gbx.LZO = new MiniLZO();
Gbx.CRC32 = new CRC32();

foreach (var fileName in args)
{
    var gbx = Gbx.Parse(fileName);

    if (gbx is not Gbx<CGameCtnChallenge> gbxMap)
    {
        Console.WriteLine($"File '{fileName}' is not a map file.");
        continue;
    }

    var converter = new NationsConverterTool(gbxMap);
    var convertedMap = converter.ConvertMap();

    convertedMap.Save("E:\\TrackmaniaUserData\\Maps\\NC2OUTPUT\\" + Path.GetFileName(fileName));
}