using NationsConverterShared.Models;
using System.Text.Json.Serialization;

namespace NationsConverterBuilder;

[JsonSerializable(typeof(ConversionSetModel))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
