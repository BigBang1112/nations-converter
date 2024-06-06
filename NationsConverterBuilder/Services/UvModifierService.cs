using GBX.NET;
using GBX.NET.Engines.Plug;
using NationsConverterBuilder.Models;

namespace NationsConverterBuilder.Services;

internal sealed class UvModifierService
{
    public void ApplyUvModifiers(CPlugVisualIndexedTriangles visual, UvModifiersModel uvModifiers)
    {
        if (uvModifiers.Scale != 1)
        {
            foreach (var texSet in visual.TexCoords)
            {
                texSet.TexCoords = texSet.TexCoords.Select(texCoord =>
                {
                    var x = texCoord.UV.X - 0.5f;
                    var y = texCoord.UV.Y - 0.5f;
                    return texCoord with { UV = new Vec2(x * uvModifiers.Scale + 0.5f, y * uvModifiers.Scale + 0.5f) };
                }).ToArray();
            }
        }

        if (uvModifiers.ScaleX != 1)
        {
            foreach (var texSet in visual.TexCoords)
            {
                texSet.TexCoords = texSet.TexCoords.Select(texCoord =>
                {
                    var x = texCoord.UV.X - 0.5f;
                    var y = texCoord.UV.Y - 0.5f;
                    return texCoord with { UV = new Vec2(x * uvModifiers.ScaleX + 0.5f, y + 0.5f) };
                }).ToArray();
            }
        }

        if (uvModifiers.ScaleY != 1)
        {
            foreach (var texSet in visual.TexCoords)
            {
                texSet.TexCoords = texSet.TexCoords.Select(texCoord =>
                {
                    var x = texCoord.UV.X - 0.5f;
                    var y = texCoord.UV.Y - 0.5f;
                    return texCoord with { UV = new Vec2(x + 0.5f, y * uvModifiers.ScaleY + 0.5f) };
                }).ToArray();
            }
        }

        if (uvModifiers.Rotate != 0)
        {
            foreach (var texSet in visual.TexCoords)
            {
                texSet.TexCoords = texSet.TexCoords.Select(texCoord =>
                {
                    var x = texCoord.UV.X - 0.5f;
                    var y = texCoord.UV.Y - 0.5f;
                    var cos = MathF.Cos(uvModifiers.Rotate * MathF.PI / 180);
                    var sin = MathF.Sin(uvModifiers.Rotate * MathF.PI / 180);
                    return texCoord with { UV = new Vec2(cos * x - sin * y + 0.5f, sin * x + cos * y + 0.5f) };
                }).ToArray();
            }
        }
    }
}
