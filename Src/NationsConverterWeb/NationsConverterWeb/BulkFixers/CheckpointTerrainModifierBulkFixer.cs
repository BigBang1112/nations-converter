using GBX.NET;
using GBX.NET.Engines.GameData;
using NationsConverterWeb.Models;

namespace NationsConverterWeb.BulkFixers;

public sealed class CheckpointTerrainModifierBulkFixer : BulkFixer<(ItemUpload, Gbx<CGameItemModel>)>
{
    private readonly AppDbContext db;

    public CheckpointTerrainModifierBulkFixer(AppDbContext db) : base(db)
    {
        this.db = db;
    }

    protected override IQueryable<ItemUpload> FilterQuery(IQueryable<ItemUpload> queryable)
    {
        return queryable.Where(x => x.BlockItem.Modifier == "GroundDefault" || x.BlockItem.Modifier == "RockyGrass");
    }

    protected override IEnumerable<(ItemUpload, Gbx<CGameItemModel>)> FilterAfterQuery(List<ItemUpload> uploads)
    {
        foreach (var upload in uploads)
        {
            var itemGbx = Gbx.Parse<CGameItemModel>(new MemoryStream(upload.Data));

            if (itemGbx.Node.WaypointType is CGameItemModel.EWaypointType.Start or CGameItemModel.EWaypointType.Finish or CGameItemModel.EWaypointType.Checkpoint)
            {
                yield return (upload, itemGbx);
            }
        }
    }

    public override async Task BulkFixAsync(IEnumerable<(ItemUpload, Gbx<CGameItemModel>)> latestUploads, CancellationToken cancellationToken = default)
    {
        foreach (var (upload, itemGbx) in latestUploads)
        {
            itemGbx.Node.WaypointType = CGameItemModel.EWaypointType.None;

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
