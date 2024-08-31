using System.ComponentModel.DataAnnotations;

namespace NationsConverterWeb.Models;

public sealed class ItemUpload
{
    public int Id { get; set; }

    [Required]
    public required byte[] Data { get; set; }

    [Required]
    [StringLength(byte.MaxValue)]
    public required string OriginalFileName { get; set; }

    [Required]
    public required DateTimeOffset LastModifiedAt { get; set; }

    [Required]
    public required DateTimeOffset UploadedAt { get; set; }

    public BlockItem BlockItem { get; set; } = default!;
}
