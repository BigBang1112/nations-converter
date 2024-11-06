using GBX.NET;
using GBX.NET.Hashing;
using GBX.NET.Tool.CLI;
using NationsConverter;
using NationsConverterCLI;
using NationsConverterShared.Converters.Json;
using System.Text.Json;
using YamlDotNet.Serialization;
using NationsConverter.Converters;

Gbx.CRC32 = new CRC32();

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonInt2Converter(), new JsonInt3Converter(), new JsonVec3Converter() }
};

//var ymlStaticContext = new YmlStaticContext();

await ToolConsole<NationsConverterTool>.RunAsync(args, new()
{
    JsonContext = new AppJsonContext(jsonOptions),
    YmlDeserializer = new DeserializerBuilder()
        .WithTypeConverter(new YmlVec3Converter()),
    YmlSerializer = new SerializerBuilder()
});