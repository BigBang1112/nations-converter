using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;

namespace NationsConverter.Converters;

internal sealed class MetadataConverter
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly uint seed;

    public MetadataConverter(CGameCtnChallenge mapIn, CGameCtnChallenge mapOut, uint seed)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.seed = seed;
    }

    public void Convert()
    {
        var metadata = new CScriptTraitsMetadata();
        metadata.Declare("MadeWithNationsConverter", true);
        metadata.Declare("NC_OriginalAuthorLogin", mapIn.AuthorLogin);
        metadata.Declare("NC_OriginalAuthorNickname", mapIn.AuthorNickname ?? string.Empty);
        metadata.Declare("NC_OriginalMapUid", mapIn.MapUid);
        metadata.Declare("NC2_IsConverted", true);
        metadata.Declare("NC2_ConvertedAt", DateTime.UtcNow.ToString("s"));
        metadata.Declare("NC2_Version", typeof(NationsConverterTool).Assembly.GetName().Version?.ToString() ?? "");
        metadata.Declare("NC2_CLI_Version", "");
        metadata.Declare("NC2_Web_Version", "");
        metadata.Declare("NC2_GBXNET_Version", "");
        metadata.Declare("NC2_Environment", mapIn.GetEnvironment() switch
        {
            "Alpine" => "Snow",
            "Speed" => "Desert",
            _ => mapIn.GetEnvironment()
        });
        metadata.Declare("NC2_PreAlpha", true);
        metadata.Declare("NC2_Seed", seed.ToString());

        metadata.CreateChunk<CScriptTraitsMetadata.Chunk11002000>().Version = 6;

        mapOut.ScriptMetadata = metadata;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043044>();
    }
}
