using GBX.NET.Tool;
using NationsConverterShared.Models;
using System.Text.Json.Serialization;

namespace NationsConverter;

public class NationsConverterConfig : Config
{
    public bool CopyItems { get; set; } = true;
    public string? UserDataFolder { get; set; }

    [ExternalFile("Snow"), JsonIgnore]
    public ConversionSetModel Snow { get; set; } = new();
    [ExternalFile("Rally"), JsonIgnore]
    public ConversionSetModel Rally { get; set; } = new();
    [ExternalFile("Desert"), JsonIgnore]
    public ConversionSetModel Desert { get; set; } = new();
    [ExternalFile("Island"), JsonIgnore]
    public ConversionSetModel Island { get; set; } = new();
    [ExternalFile("Bay"), JsonIgnore]
    public ConversionSetModel Bay { get; set; } = new();
    [ExternalFile("Coast"), JsonIgnore]
    public ConversionSetModel Coast { get; set; } = new();
    [ExternalFile("Stadium"), JsonIgnore]
    public ConversionSetModel Stadium { get; set; } = new();
}
