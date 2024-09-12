using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

public sealed class BlockItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(byte.MaxValue)]
    public required string FileName { get; set; }

    [Required]
    [StringLength(byte.MaxValue)]
    public required string Modifier { get; set; }

    [Required]
    public required int Variant { get; set; }

    [Required]
    public required int SubVariant { get; set; }

    public int BlockId { get; set; }
    public Block Block { get; set; } = default!;

    [Range(0, 100)]
    public int Value { get; set; }

    public bool JustResave { get; set; }

    public ICollection<ItemUpload> Uploads { get; set; } = [];
}
