using GBX.NET.Tool;

namespace NationsConverter;

public class NationsConverterConfig : Config
{
    public bool CopyItems { get; set; } = false;
    public string? UserDataFolder { get; set; }
    public bool IncludeDecoration { get; set; }
    public bool UseNewWood { get; set; }
}
