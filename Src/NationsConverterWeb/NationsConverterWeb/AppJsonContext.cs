using NationsConverterShared.Converters.Json;
using NationsConverterShared.Models;
using System.Text.Json.Serialization;

namespace NationsConverterWeb;

[JsonSerializable(typeof(ConversionSetModel))]
[JsonSerializable(typeof(ItemInfoModel))]
[JsonSourceGenerationOptions(
    Converters = [typeof(JsonInt2Converter), typeof(JsonInt3Converter), typeof(JsonVec3Converter)],
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class AppJsonContext : JsonSerializerContext;
