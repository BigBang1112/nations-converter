namespace NationsConverterWeb.Models.Dto;

public sealed class BlockDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string EnvironmentId { get; set; }
    public required string CategoryId { get; set; }
    public required string SubCategoryId { get; set; }
    public required User? AssignedTo { get; set; }
    public required int? AssignedToId { get; set; }
    public required DateTimeOffset? AssignedAt { get; set; }
    public required bool HasUpload { get; set; }
    public required bool IsDone { get; set; }
}
