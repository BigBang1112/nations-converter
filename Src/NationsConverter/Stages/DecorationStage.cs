using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using NationsConverter.Models;
using System.Text.RegularExpressions;

namespace NationsConverter.Stages;

internal sealed partial class DecorationStage : EnvironmentStageBase
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly ManualConversionSetModel conversionSet;
    private readonly CustomContentManager customContentManager;
    private readonly ILogger logger;

    private readonly bool includeDecorationItem;

    [GeneratedRegex(@"(Sunrise|Day|Sunset|Night)", RegexOptions.IgnoreCase)]
    private static partial Regex MoodRegex();

    public DecorationStage(
        CGameCtnChallenge mapIn,
        CGameCtnChallenge mapOut,
        ManualConversionSetModel conversionSet,
        NationsConverterConfig config,
        CustomContentManager customContentManager,
        ILogger logger) : base(mapIn)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.conversionSet = conversionSet;
        this.customContentManager = customContentManager;
        this.logger = logger;

        includeDecorationItem = config.IncludeDecoration && Environment != "Stadium";
    }

    public void Convert()
    {
        var mood = MoodRegex().Match(mapIn.Decoration.Id).Value;

        if (string.IsNullOrEmpty(mood))
        {
            mood = "Day";
        }

        mood = string.Concat(mood[0].ToString().ToUpper(), mood.AsSpan(1));

        var mapBase = includeDecorationItem
            ? "NoStadium48x48"
            : "48x48Screen155";

        if (includeDecorationItem)
        {
            var blockSize = mapIn.Collection.GetValueOrDefault().GetBlockSize();
            mapOut.Size = new((int)(mapIn.Size.X * (blockSize.X / 32f)), 40, (int)(mapIn.Size.Z * (blockSize.Z / 32f)));
        }

        mapOut.Decoration = new($"{mapBase}{mood}", 26, "Nadeo");

        logger.LogInformation("Decoration: {Name}", mapOut.Decoration.Id);
        logger.LogInformation("Size: {Size}", mapOut.Size);

        if (includeDecorationItem)
        {
            var sizeStr = $"{mapIn.Size.X}x{mapIn.Size.Y}x{mapIn.Size.Z}";
            var itemPath = Path.Combine("Decorations", $"{sizeStr}.Item.Gbx");

            var yOffset = conversionSet.DecorationYOffset;
            if (conversionSet.Decorations.TryGetValue(sizeStr, out var deco))
            {
                yOffset += deco.YOffset;
            }

            customContentManager.PlaceItem(itemPath, (0, yOffset, 0), (0, 0, 0), lightmapQuality: LightmapQuality.Lowest);

            logger.LogInformation("Placed decoration item ({Size}).", sizeStr);
        }

        var voidSize = includeDecorationItem ? mapOut.Size : mapIn.Size;

        // if Stadium, expand void size to be under the height transition

        var voidOffset = new Int3((mapOut.Size.X - mapIn.Size.X) / 2, 0, (mapOut.Size.Z - mapIn.Size.Z) / 2);

        for (var x = 0; x < voidSize.X; x++)
        {
            for (var z = 0; z < voidSize.Z; z++)
            {
                PlaceVoidBlock(voidOffset.X + x, voidOffset.Z + z);
            }
        }

        logger.LogInformation("Placed {SizeX}x{SizeZ} void.", voidSize.X, voidSize.Z);

        if (Environment == "Stadium")
        {
            PlaceTransitionGrass();
            PlaceTransitionVoid();
        }
    }

    private void PlaceVoidBlock(int x, int z)
    {
        var block = customContentManager.PlaceBlock(@"Misc\Void", (x, 9, z), Direction.North, isGround: true);
        block.LightmapQuality = LightmapQuality.Lowest;
    }

    private void PlaceTransitionGrass()
    {
        const string grassCorner = @"Misc\Border\GrassCorner.Item.Gbx";
        const string grassEdge = @"Misc\Border\GrassEdge.Item.Gbx";

        var mapSize = mapIn.Size;
        var offset = new Int3(224, 8, 224);
        var blockSize = new Int3(32, 8, 32);

        customContentManager.PlaceItem(grassCorner, offset, default);

        for (var x = 0; x < mapSize.X; x++)
        {
            customContentManager.PlaceItem(grassEdge, offset + ((x + 1) * blockSize.X, 0, 0), default);
        }

        customContentManager.PlaceItem(grassCorner, offset + ((mapSize.X + 2) * blockSize.X, 0, 0), (AdditionalMath.ToRadians(-90), 0, 0));

        for (var z = 0; z < mapSize.Z; z++)
        {
            customContentManager.PlaceItem(grassEdge, offset + ((mapSize.X + 2) * blockSize.X, 0, (z + 1) * blockSize.Z), (AdditionalMath.ToRadians(-90), 0, 0));
        }

        customContentManager.PlaceItem(grassCorner, offset + ((mapSize.X + 2) * blockSize.X, 0, (mapSize.Z + 2) * blockSize.Z), (AdditionalMath.ToRadians(180), 0, 0));

        for (var x = 0; x < mapSize.X; x++)
        {
            customContentManager.PlaceItem(grassEdge, offset + ((mapSize.X + 1) * blockSize.X - x * blockSize.X, 0, (mapSize.Z + 2) * blockSize.Z), (AdditionalMath.ToRadians(180), 0, 0));
        }

        customContentManager.PlaceItem(grassCorner, offset + (0, 0, (mapSize.Z + 2) * blockSize.Z), (AdditionalMath.ToRadians(90), 0, 0));

        for (var z = 0; z < mapSize.Z; z++)
        {
            customContentManager.PlaceItem(grassEdge, offset + (0, 0, (mapSize.Z + 1) * blockSize.Z - z * blockSize.Z), (AdditionalMath.ToRadians(90), 0, 0));
        }
    }

    private void PlaceTransitionVoid()
    {
        var voidLength = 8;

        var mapSize = mapIn.Size;

        // Edges

        for (var x = 0; x < mapSize.X; x++)
        {
            for (var z = 0; z < voidLength; z++)
            {
                PlaceVoidBlock(voidLength + x, z);
            }
        }

        for (var x = 0; x < voidLength; x++)
        {
            for (var z = 0; z < mapSize.Z; z++)
            {
                PlaceVoidBlock(x + mapSize.X + voidLength, voidLength + z);
            }
        }

        for (var x = 0; x < mapSize.X; x++)
        {
            for (var z = 0; z < voidLength; z++)
            {
                PlaceVoidBlock(x + voidLength, z + mapSize.Z + voidLength);
            }
        }

        for (var x = 0; x < voidLength; x++)
        {
            for (var z = 0; z < mapSize.Z; z++)
            {
                PlaceVoidBlock(x, z + voidLength);
            }
        }

        // Corners

        for (var x = 0; x < voidLength; x++)
        {
            for (var z = 0; z < voidLength; z++)
            {
                PlaceVoidBlock(x, voidLength - z - 1);
            }
        }

        for (var x = 0; x < voidLength; x++)
        {
            for (var z = 0; z < voidLength; z++)
            {
                PlaceVoidBlock(x + mapSize.X + voidLength, z);
            }
        }

        for (var x = 0; x < voidLength; x++)
        {
            for (var z = 0; z < voidLength; z++)
            {
                PlaceVoidBlock(x + mapSize.X + voidLength, z + mapSize.Z + voidLength);
            }
        }

        for (var x = 0; x < voidLength; x++)
        {
            for (var z = 0; z < voidLength; z++)
            {
                PlaceVoidBlock(x, z + mapSize.Z + voidLength);
            }
        }
    }
}
