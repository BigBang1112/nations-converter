using GBX.NET;
using GBX.NET.Hashing;
using GBX.NET.Tool.CLI;
using NationsConverter;
using NationsConverterCLI;
using NationsConverterShared.Converters.Json;
using System.Text.Json;

Gbx.CRC32 = new CRC32();

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonInt3Converter() }
};

await ToolConsole<NationsConverterTool>.RunAsync(args, new()
{
    JsonSerializerContext = new AppJsonContext(jsonOptions)
});