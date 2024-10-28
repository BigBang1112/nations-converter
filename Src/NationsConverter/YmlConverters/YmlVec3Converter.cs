using GBX.NET;
using System.Globalization;
using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace NationsConverter.YmlConverters;

public sealed class YmlVec3Converter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Vec3) || type == typeof(Vec3?);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        _ = parser.Consume<SequenceStart>();

        var x = float.Parse(parser.Consume<Scalar>().Value);
        var y = float.Parse(parser.Consume<Scalar>().Value);
        var z = float.Parse(parser.Consume<Scalar>().Value);

        _ = parser.Consume<SequenceEnd>();

        return new Vec3(x, y, z);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.Emit(new Scalar("~"));
            return;
        }

        var val = (Vec3)value!;

        emitter.Emit(new SequenceStart(default, default, isImplicit: true, SequenceStyle.Flow));
        emitter.Emit(new Scalar(val.X.ToString(CultureInfo.InvariantCulture)));
        emitter.Emit(new Scalar(val.Y.ToString(CultureInfo.InvariantCulture)));
        emitter.Emit(new Scalar(val.Z.ToString(CultureInfo.InvariantCulture)));
        emitter.Emit(new SequenceEnd());
    }
}