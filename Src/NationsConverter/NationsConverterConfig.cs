using GBX.NET.Tool;
using NationsConverterShared.Models;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace NationsConverter;

public class NationsConverterConfig : Config
{
    public bool CopyItems { get; set; } = true;
    public string? UserDataFolder { get; set; }

    [ExternalFile("Snow"), JsonIgnore, YamlIgnore]
    public ConversionSetModel Snow { get; set; } = new();
    [ExternalFile("Rally"), JsonIgnore, YamlIgnore]
    public ConversionSetModel Rally { get; set; } = new();
    [ExternalFile("Desert"), JsonIgnore, YamlIgnore]
    public ConversionSetModel Desert { get; set; } = new();
    [ExternalFile("Island"), JsonIgnore, YamlIgnore]
    public ConversionSetModel Island { get; set; } = new();
    [ExternalFile("Bay"), JsonIgnore, YamlIgnore]
    public ConversionSetModel Bay { get; set; } = new();
    [ExternalFile("Coast"), JsonIgnore, YamlIgnore]
    public ConversionSetModel Coast { get; set; } = new();
    [ExternalFile("Stadium"), JsonIgnore, YamlIgnore]
    public ConversionSetModel Stadium { get; set; } = new();
}
