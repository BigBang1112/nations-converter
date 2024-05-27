using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using NationsConverterBuilder.Models;

namespace NationsConverterBuilder.Services;

internal sealed class GeneralBuildService
{
    private readonly SetupService setupService;
    private readonly ItemMakerService itemMaker;

    public GeneralBuildService(SetupService setupService, ItemMakerService itemMaker)
    {
        this.setupService = setupService;
        this.itemMaker = itemMaker;
    }

    public async Task BuildAsync(CancellationToken cancellationToken = default)
    {
        foreach (var collection in setupService.Collections.Values)
        {
            await setupService.SetupCollectionAsync(collection, cancellationToken);

            RecurseBlockDirectories(collection.BlockDirectories);
        }
    }

    private void RecurseBlockDirectories(IDictionary<string, BlockDirectoryModel> dirs)
    {
        foreach (var (dirName, dir) in dirs)
        {
            RecurseBlockDirectories(dir.Directories);

            foreach (var (name, block) in dir.Blocks)
            {
                block.Node = (CGameCtnBlockInfo)Gbx.ParseNode(block.GbxFilePath)!;

                foreach (var groundMobilSubVariants in block.Node.GroundMobils!)
                {
                    GenerateSubVariants(groundMobilSubVariants, "Ground");
                }

                foreach (var airMobilSubVariants in block.Node.AirMobils!)
                {
                    GenerateSubVariants(airMobilSubVariants, "Air");
                }
            }
        }
    }

    private void GenerateSubVariants(External<CSceneMobil>[] subVariants, string type)
    {
        foreach (var subVariant in subVariants)
        {
            if (subVariant.Node?.Item?.Solid?.Tree is not CPlugSolid solid)
            {
                continue;
            }

            var item = itemMaker.Build(solid);

            // save into correct folders
        }
    }
}
