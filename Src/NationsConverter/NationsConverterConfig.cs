using GBX.NET.Tool;
using NationsConverterShared.Models;

namespace NationsConverter;

public class NationsConverterConfig : Config
{
    public bool CopyItems { get; set; } = true;
    public string? UserDataFolder { get; set; }

    [ExternalFile("Snow")]
    public ConversionSetModel Snow { get; set; } = new();
    [ExternalFile("Rally")]
    public ConversionSetModel Rally { get; set; } = new();
    [ExternalFile("Desert")]
    public ConversionSetModel Desert { get; set; } = new();
    [ExternalFile("Island")]
    public ConversionSetModel Island { get; set; } = new();
    [ExternalFile("Bay")]
    public ConversionSetModel Bay { get; set; } = new();
    [ExternalFile("Coast")]
    public ConversionSetModel Coast { get; set; } = new();
    [ExternalFile("Stadium")]
    public ConversionSetModel Stadium { get; set; } = new();
}
