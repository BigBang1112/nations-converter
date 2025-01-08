using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.LZO;

Gbx.LZO = new Lzo();

var macroblock = Gbx.ParseNode<CGameCtnMacroBlockInfo>(args[0]);

foreach (var obj in macroblock.ObjectSpawns ?? [])
{
    var pos = new Vec3(
        MathF.Round(obj.AbsolutePositionInMap.X, 4),
        MathF.Round(obj.AbsolutePositionInMap.Y, 4),
        MathF.Round(obj.AbsolutePositionInMap.Z, 4));
    var rot = new Vec3(
        MathF.Round(obj.PitchYawRoll.X / MathF.PI * 180, 4),
        MathF.Round(obj.PitchYawRoll.Y / MathF.PI * 180, 4),
        MathF.Round(obj.PitchYawRoll.Z / MathF.PI * 180, 4));

    Console.WriteLine($"- Name: {obj.ItemModel?.Id}");
    if (pos.X != 0)
    {
        Console.WriteLine($"  OffsetX: {pos.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }
    if (pos.Y != 0)
    {
        Console.WriteLine($"  OffsetY: {pos.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }
    if (pos.Z != 0)
    {
        Console.WriteLine($"  OffsetZ: {pos.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }
    if (rot.X != 0)
    {
        Console.WriteLine($"  RotX: {rot.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }
    if (rot.Y != 0)
    {
        Console.WriteLine($"  RotY: {rot.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }
    if (rot.Z != 0)
    {
        Console.WriteLine($"  RotZ: {rot.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }
}