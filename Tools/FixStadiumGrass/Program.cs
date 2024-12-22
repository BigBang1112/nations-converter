using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GBX.NET.LZO;

Gbx.LZO = new Lzo();

foreach (var filePath in Directory.EnumerateFiles(args[0], "*.Gbx", SearchOption.AllDirectories))
{
    var itemGbx = Gbx.Parse<CGameItemModel>(filePath);

    if (itemGbx.Node.EntityModelEdition is not CGameCommonItemEntityModelEdition entityModelEdition
        || entityModelEdition.MeshCrystal is null)
    {
        continue;
    }

    var changed = false;

    foreach (var material in entityModelEdition.MeshCrystal.Materials)
    {
        if (material.MaterialUserInst?.Link == "Stadium\\Media\\Material\\Grass")
        {
            material.MaterialUserInst.SurfacePhysicId = CPlugSurface.MaterialId.Green;
            changed = true;
        }
    }

    if (!changed)
    {
        continue;
    }

    using var fs = File.Create(filePath);
    itemGbx.Save(fs);
}