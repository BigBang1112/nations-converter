using NationsConverter;
using NationsConverterShared.Models;
using System.Text.Json.Serialization;

namespace NationsConverterCLI;

[JsonSerializable(typeof(NationsConverterConfig))]
[JsonSerializable(typeof(ConversionSetModel))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
