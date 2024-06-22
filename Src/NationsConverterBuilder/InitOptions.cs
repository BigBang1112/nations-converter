using NationsConverterBuilder.Models;

namespace NationsConverterBuilder;

internal sealed class InitOptions
{
    public Dictionary<string, MaterialModel> Materials { get; set; } = [];
}
