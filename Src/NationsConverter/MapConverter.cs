using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;

namespace NationsConverter;

internal sealed class MapConverter
{
    private readonly CGameCtnChallenge map;
    private readonly CGameCtnChallenge convertedMap;
    private readonly NationsConverterConfig config;
    private readonly ILogger logger;

    public MapConverter(CGameCtnChallenge map, CGameCtnChallenge convertedMap, NationsConverterConfig config, ILogger logger)
    {
        this.map = map;
        this.convertedMap = convertedMap;
        this.config = config;
        this.logger = logger;
    }

    public void Convert()
    {
        var environment = map.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => map.GetEnvironment()
        };

        var blockSize = map.Collection.GetValueOrDefault().GetBlockSize();

        var conversions = environment switch
        {
            "Snow" => config.Snow,
            "Rally" => config.Rally,
            "Desert" => config.Desert,
            "Island" => config.Island,
            "Bay" => config.Bay,
            "Coast" => config.Coast,
            "Stadium" => config.Stadium, // should not be always Solid category
            _ => throw new ArgumentException("Environment not supported")
        };

        foreach (var block in map.GetBlocks())
        {
            if (block.Variant is null || block.SubVariant is null)
            {
                continue;
            }

            if (!conversions.TryGetValue(block.Name, out var conversion))
            {
                continue;
            }

            Int3 blockCoordSize;
            int maxSubVariants;

            if (block.IsClip)
            {
                // Resolve later
                continue;
            }

            if (block.IsGround)
            {
                blockCoordSize = conversion.GetProperty(x => x.Ground, x => x.Size);
                var maxVariants = conversion.GetProperty(x => x.Ground, x => x.Variants);

                if (block.Variant >= maxVariants)
                {
                    throw new ArgumentException("Block variant exceeds max variants");
                }

                maxSubVariants = conversion.GetProperty(x => x.Ground, x => x.SubVariants?[block.Variant.Value]);
            }
            else
            {
                blockCoordSize = conversion.GetProperty(x => x.Air, x => x.Size);
                var maxVariants = conversion.GetProperty(x => x.Air, x => x.Variants);

                if (block.Variant >= maxVariants)
                {
                    throw new ArgumentException("Block variant exceeds max variants");
                }

                maxSubVariants = conversion.GetProperty(x => x.Air, x => x.SubVariants?[block.Variant.Value]);
            }

            if (block.SubVariant >= maxSubVariants)
            {
                throw new ArgumentException("Block sub variant exceeds max sub variants");
            }

            var modifierType = block.IsGround ? "Ground" : "Air";

            var subCategory = "Modless";

            var dirPath = Path.Combine("NC2", "Solid", subCategory, "MM_Collision", environment, conversion.PageName, block.Name);
            var itemPath = Path.Combine(dirPath, $"{modifierType}_{block.Variant.Value}_{block.SubVariant.Value}.Item.Gbx");

            var pos = block.Direction switch
            {
                Direction.East => block.Coord + (blockCoordSize.Z, 0, 0),
                Direction.South => block.Coord + (blockCoordSize.X, 0, blockCoordSize.Z),
                Direction.West => block.Coord + (0, 0, blockCoordSize.X),
                _ => block.Coord
            };

            var dir = -(int)block.Direction * MathF.PI / 2;

            logger.LogInformation("Placing item ({BlockName}) at {Pos} with rotation {Dir}...", block.Name, pos, dir);

            convertedMap.PlaceAnchoredObject(
                new(itemPath.Replace('/', '\\'), 26, "akPfIM0aSzuHuaaDWptBbQ"),
                    pos * blockSize, (dir, 0, 0));
        }
    }
}