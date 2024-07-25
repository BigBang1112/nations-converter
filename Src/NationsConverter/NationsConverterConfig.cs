using GBX.NET.Tool;
using NationsConverterShared.Models;

namespace NationsConverter;

public class NationsConverterConfig : Config
{
    [ExternalFile("Snow")]
    public Dictionary<string, ConversionModel> Snow { get; set; } = [];
    [ExternalFile("Rally")]
    public Dictionary<string, ConversionModel> Rally { get; set; } = [];
    [ExternalFile("Desert")]
    public Dictionary<string, ConversionModel> Desert { get; set; } = [];
    [ExternalFile("Island")]
    public Dictionary<string, ConversionModel> Island { get; set; } = [];
    [ExternalFile("Bay")]
    public Dictionary<string, ConversionModel> Bay { get; set; } = [];
    [ExternalFile("Coast")]
    public Dictionary<string, ConversionModel> Coast { get; set; } = [];
    [ExternalFile("Stadium")]
    public Dictionary<string, ConversionModel> Stadium { get; set; } = [];
}
