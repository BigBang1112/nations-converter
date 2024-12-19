using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;
using System.Reflection;

namespace NationsConverter.Stages;

internal sealed class MetadataStage : EnvironmentStageBase
{
    private readonly CGameCtnChallenge mapIn;
    private readonly CGameCtnChallenge mapOut;
    private readonly NationsConverterConfig config;
    private readonly uint seed;

    public MetadataStage(CGameCtnChallenge mapIn, CGameCtnChallenge mapOut, NationsConverterConfig config, uint seed) : base(mapIn)
    {
        this.mapIn = mapIn;
        this.mapOut = mapOut;
        this.config = config;
        this.seed = seed;
    }

    public void Convert()
    {
        var metadata = new CScriptTraitsMetadata();
        metadata.CreateChunk<CScriptTraitsMetadata.Chunk11002000>().Version = 6;

        metadata.Declare("MadeWithNationsConverter", true);
        metadata.Declare("NC_OriginalAuthorLogin", mapIn.AuthorLogin);
        metadata.Declare("NC_OriginalAuthorNickname", mapIn.AuthorNickname ?? string.Empty);
        metadata.Declare("NC_OriginalMapUid", mapIn.MapUid);
        metadata.Declare("NC2_IsConverted", true);
        metadata.Declare("NC2_ConvertedAt", DateTime.UtcNow.ToString("s"));
        metadata.Declare("NC2_Version", typeof(NationsConverterTool).Assembly.GetName().Version?.ToString() ?? "");
        metadata.Declare("NC2_CLI_Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "");
        metadata.Declare("NC2_Web_Version", "");
        metadata.Declare("NC2_GBXNET_Version", typeof(CGameCtnChallenge).Assembly.GetName().Version?.ToString() ?? "");
        metadata.Declare("NC2_Environment", Environment);
        metadata.Declare("NC2_PreAlpha", true);
        metadata.Declare("NC2_Seed", seed.ToString());
        metadata.Declare("NC2_Category", config.GetUsedCategory(Environment));
        metadata.Declare("NC2_SubCategory", config.GetUsedSubCategory(Environment));
        metadata.Declare("NC2_IsTM2", mapIn.CanBeGameVersion(GameVersion.MP4));

        mapOut.ScriptMetadata = metadata;
        mapOut.CreateChunk<CGameCtnChallenge.Chunk03043044>();
    }
}
