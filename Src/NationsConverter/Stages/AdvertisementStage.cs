using GBX.NET;
using GBX.NET.Engines.Game;

namespace NationsConverter.Stages;

public sealed class AdvertisementStage
{
    private readonly CGameCtnChallenge mapOut;

    public AdvertisementStage(CGameCtnChallenge mapOut)
    {
        this.mapOut = mapOut;
    }

    public void Convert()
    {
        foreach (var block in mapOut.GetBlocks())
        {
            if (block.Name is not "RoadTechStart" and not "RoadTechFinish" and not "RoadTechMultilap")
            {
                continue;
            }

            var skin = new CGameCtnBlockSkin
            {
                Text = "!4",
                PackDesc = new PackDesc(@"Skins\Any\NC2\NCStartBlock2.jpg", LocatorUrl: "https://download.dashmap.live/6a43df20-cd1a-4b3b-87b9-a6835a9b416d/NCStartBlock2.jpg")
            };
            skin.CreateChunk<CGameCtnBlockSkin.Chunk03059002>();

            block.Skin = skin;
        }
    }
}
