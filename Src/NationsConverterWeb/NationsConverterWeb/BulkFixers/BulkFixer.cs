using Microsoft.EntityFrameworkCore;
using NationsConverterWeb.Models;

namespace NationsConverterWeb.BulkFixers;

public abstract class BulkFixer<T>
{
    private readonly AppDbContext db;

    protected BulkFixer(AppDbContext db)
    {
        this.db = db;
    }

    protected virtual IQueryable<ItemUpload> FilterQuery(IQueryable<ItemUpload> queryable) => queryable;

    protected abstract IEnumerable<T> FilterAfterQuery(List<ItemUpload> uploads);

    public async Task<IEnumerable<T>> GetFilteredAsync(CancellationToken cancellationToken = default)
    {
        var latestBlockItemUploads = db.ItemUploads
            .Include(x => x.BlockItem)
                .ThenInclude(x => x.Block)
            .GroupBy(x => x.BlockItem)
            .Select(x => x.OrderByDescending(x => x.UploadedAt).First());

        var retrieved = await FilterQuery(latestBlockItemUploads).ToListAsync(cancellationToken);
        return FilterAfterQuery(retrieved);
    }

    public abstract Task BulkFixAsync(IEnumerable<T> latestUploads, CancellationToken cancellationToken = default);
}
