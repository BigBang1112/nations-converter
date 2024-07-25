using NationsConverter;
using System.Text.Json.Serialization;

namespace NationsConverterCLI;

[JsonSerializable(typeof(NationsConverterConfig))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
