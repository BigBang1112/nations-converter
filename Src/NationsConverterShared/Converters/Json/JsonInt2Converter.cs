using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using GBX.NET;

namespace NationsConverterShared.Converters.Json;

public class JsonInt2Converter : JsonConverter<Int2>
{
    public override Int2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(Int2));

        reader.Read();
        var x = reader.GetInt32();
        reader.Read();
        var y = reader.GetInt32();
        reader.Read();

        return new Int2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Int2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}
