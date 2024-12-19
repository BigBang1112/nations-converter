using NationsConverter;
using NationsConverterShared.Converters.Json;
using NationsConverterShared.Models;
using System.Text.Json.Serialization;

namespace NationsConverterCLI;

[JsonSerializable(typeof(NationsConverterConfig))]
[JsonSerializable(typeof(ConversionSetModel))]
[JsonSourceGenerationOptions(
    Converters = [typeof(JsonInt2Converter), typeof(JsonInt3Converter), typeof(JsonVec3Converter)],
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class AppJsonContext : JsonSerializerContext;
