using GBX.NET;
using GBX.NET.Engines.Plug;

namespace NationsConverterBuilder.Extensions;

public static class CPlugMaterialUserInstExtensions
{
    public static CPlugMaterialUserInst Create(string link = "Stadium\\Media\\Material\\PlatformTech", CPlugSurface.MaterialId surface = CPlugSurface.MaterialId.Asphalt, int[]? color = null)
    {
        var csts = color is null ? null : new CPlugMaterialUserInst.Cst[]
        {
            new()
            {
                U01 = "TargetColor",
                U02 = "Real",
                U03 = 3,
            }
        };

        var material = new CPlugMaterialUserInst
        {
            IsUsingGameMaterial = true,
            Link = link,
            SurfacePhysicId = (byte)surface,
            TextureSizeInMeters = 1,
            Csts = csts,
            Color = color
        };
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD000>().Version = 11;
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD001>().Version = 5;
        material.CreateChunk<CPlugMaterialUserInst.Chunk090FD002>();
        return material;
    }
}
