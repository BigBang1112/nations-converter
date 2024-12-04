using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using NationsConverterWeb.Models;

using static GBX.NET.Engines.Plug.CPlugSurface.MaterialId;

namespace NationsConverterWeb.BulkFixers;

public sealed class RevertMaterialPhysicsBulkFixer : BulkFixer<(ItemUpload, Gbx<CGameItemModel>)>
{
    private readonly Dictionary<CPlugSurface.MaterialId, CPlugSurface.MaterialId> mapping = new()
    {
        [WetPavement] = Asphalt,
        [WetDirtRoad] = Dirt
    };

    private readonly AppDbContext db;

    public RevertMaterialPhysicsBulkFixer(AppDbContext db) : base(db)
    {
        this.db = db;
    }

    protected override IQueryable<ItemUpload> FilterQuery(IQueryable<ItemUpload> queryable)
    {
        return queryable.Where(x => x.BlockItem.Block.EnvironmentId == "Rally");
    }

    protected override IEnumerable<(ItemUpload, Gbx<CGameItemModel>)> FilterAfterQuery(List<ItemUpload> uploads)
    {
        foreach (var upload in uploads)
        {
            var itemGbx = Gbx.Parse<CGameItemModel>(new MemoryStream(upload.Data));

            if (itemGbx.Node.EntityModelEdition is not CGameCommonItemEntityModelEdition entityModelEdition
                || entityModelEdition.MeshCrystal is null)
            {
                continue;
            }

            if (entityModelEdition.MeshCrystal.Materials
                .Any(x => x.MaterialUserInst is not null
                    && mapping.ContainsKey(x.MaterialUserInst.SurfacePhysicId)))
            {
                yield return (upload, itemGbx);
            }
        }
    }

    public override async Task BulkFixAsync(IEnumerable<(ItemUpload, Gbx<CGameItemModel>)> latestUploads, CancellationToken cancellationToken = default)
    {
        foreach (var (upload, itemGbx) in latestUploads)
        {
            if (itemGbx.Node.EntityModelEdition is not CGameCommonItemEntityModelEdition entityModelEdition
                || entityModelEdition.MeshCrystal is null)
            {
                continue;
            }

            foreach (var material in entityModelEdition.MeshCrystal.Materials)
            {
                if (material.MaterialUserInst is not null
                    && mapping.TryGetValue(material.MaterialUserInst.SurfacePhysicId, out var newMaterialId))
                {
                    material.MaterialUserInst.SurfacePhysicId = newMaterialId;
                }
            }

            using var ms = new MemoryStream();
            itemGbx.Save(ms);

            db.ItemUploads.Add(new ItemUpload
            {
                Data = ms.ToArray(),
                OriginalFileName = upload.OriginalFileName,
                LastModifiedAt = DateTimeOffset.UtcNow,
                UploadedAt = DateTimeOffset.UtcNow,
                BlockItem = upload.BlockItem
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
