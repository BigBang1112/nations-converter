using GBX.NET;
using NationsConverterBuilder2.Models;

namespace NationsConverterBuilder2.Services;

internal sealed class UvModifierService
{
    public void ApplyUvModifiers(Vec2[] uvs, UvModifiersModel uvModifiers)
    {
        if (uvModifiers.Scale != 1)
        {
            for (var i = 0; i < uvs.Length; i++)
            {
                var x = uvs[i].X - 0.5f;
                var y = uvs[i].Y - 0.5f;
                uvs[i] = new Vec2(x * uvModifiers.Scale + 0.5f, y * uvModifiers.Scale + 0.5f);
            }
        }

        if (uvModifiers.ScaleX != 1)
        {
            for (var i = 0; i < uvs.Length; i++)
            {
                var x = uvs[i].X - 0.5f;
                var y = uvs[i].Y - 0.5f;
                uvs[i] = new Vec2(x * uvModifiers.ScaleX + 0.5f, y + 0.5f);
            }
        }

        if (uvModifiers.ScaleY != 1)
        {
            for (var i = 0; i < uvs.Length; i++)
            {
                var x = uvs[i].X - 0.5f;
                var y = uvs[i].Y - 0.5f;
                uvs[i] = new Vec2(x + 0.5f, y * uvModifiers.ScaleY + 0.5f);
            }
        }

        if (uvModifiers.Rotate != 0)
        {
            for (var i = 0; i < uvs.Length; i++)
            {
                var x = uvs[i].X - 0.5f;
                var y = uvs[i].Y - 0.5f;
                var cos = MathF.Cos(uvModifiers.Rotate * MathF.PI / 180);
                var sin = MathF.Sin(uvModifiers.Rotate * MathF.PI / 180);
                uvs[i] = new Vec2(cos * x - sin * y + 0.5f, sin * x + cos * y + 0.5f);
            }
        }

        if (uvModifiers.TranslateX != 0)
        {
            for (var i = 0; i < uvs.Length; i++)
            {
                var x = uvs[i].X + uvModifiers.TranslateX;
                uvs[i] = new Vec2(x, uvs[i].Y);
            }
        }

        if (uvModifiers.TranslateY != 0)
        {
            for (var i = 0; i < uvs.Length; i++)
            {
                var y = uvs[i].Y + uvModifiers.TranslateY;
                uvs[i] = new Vec2(uvs[i].X, y);
            }
        }
    }
}
