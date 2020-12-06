using GBX.NET;
using GBX.NET.BlockInfo;
using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NationsConverter.Stages
{
    public class GroundPlacer : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters, ConverterTemporary temporary)
        {
            map.Blocks.ForEach(x =>
            {
                x.Coord += (8, 0, 8); // Shift the block by 8x0x8 positions to center the blocks for the new Stadium

                if (version >= GameVersion.TM2)
                    x.Coord -= (0, 8, 0);
            });

            map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Materials/GrassTexGreenPhy.Mat.Gbx", "Materials", true); // False crashes GBX.NET
            map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Items/NationsConverter/z_terrain/w_grass/GrassGround.Item.Gbx", "Items/NationsConverter/z_terrain/w_grass");

            var dirtBlocks = new string[] { "StadiumDirt", "StadiumDirtHill", "StadiumPool", "StadiumWater" };

            var grassCoords = new List<Int3>();

            for (var x = 0+8; x < 32+8; x++)
            {
                for (var z = 0 + 8; z < 32 + 8; z++)
                {
                    var dirtBlockExists = false;

                    foreach (var groundBlock in map.Blocks.Where(o => o.Coord == (x, 0, z)
                    || (version <= GameVersion.TMUF &&
                       (o.Coord == (x - 1, 1, z - 1) // TMNF hill
                    || o.Coord == (x - 1, -1, z - 1))))) // TMNF base
                    {
                        if (dirtBlocks.Contains(groundBlock.Name))
                        {
                            dirtBlockExists = true;
                            break;
                        }
                    }

                    if (!dirtBlockExists)
                    {
                        grassCoords.Add((x, 1, z));

                        map.PlaceAnchoredObject(
                            (@"NationsConverter\z_terrain\w_grass\GrassGround.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
                            (x, 1, z) * map.Collection.GetBlockSize(),
                            (0, 0, 0));
                    }
                }
            }

            var blocks = map.Blocks.ToArray();

            map.Blocks = blocks.AsParallel().Where(x =>
            {
                if (x.Name == "StadiumDirt")
                    temporary.DirtCoords.Add(x.Coord);

                if (x.Name == "StadiumDirt" || x.Name == "StadiumDirtHill" || x.Name == "StadiumDirtBorder")
                {
                    var dirtBlock = x;

                    var offset = default(Int3);
                    if(version <= GameVersion.TMUF)
                    {
                        if (x.Name == "StadiumDirt")
                            offset += (0, 1, 0);
                        else if (x.Name == "StadiumDirtHill")
                            offset -= (0, 1, 0);
                        else if (x.Name == "StadiumDirtBorder")
                            offset += (0, 1, 0);
                    }

                    foreach (var block in blocks)
                    {
                        if (parameters.Definitions.TryGetValue(block.Name, out Conversion[] variants))
                        {
                            if (variants != null)
                            {
                                var variant = block.Variant.GetValueOrDefault();

                                if (variants.Length > variant)
                                {
                                    var conversion = variants[variant];

                                    if (conversion != null) // If the variant actually has a conversion
                                    {
                                        if (conversion.RemoveGround)
                                        {
                                            if (BlockInfoManager.BlockModels.TryGetValue(block.Name, out BlockModel model))
                                            {
                                                var center = default(Vec3);
                                                var allCoords = new Int3[model.Ground.Length];
                                                var newCoords = new Vec3[model.Ground.Length];
                                                var newMin = default(Vec3);

                                                if (model.Ground.Length > 1)
                                                {
                                                    allCoords = model.Ground.Select(b => (Int3)b.Coord).ToArray();
                                                    var min = new Int3(allCoords.Select(c => c.X).Min(), allCoords.Select(c => c.Y).Min(), allCoords.Select(c => c.Z).Min());
                                                    var max = new Int3(allCoords.Select(c => c.X).Max(), allCoords.Select(c => c.Y).Max(), allCoords.Select(c => c.Z).Max());
                                                    var size = max - min + (1, 1, 1);
                                                    center = (min + max) * .5f;

                                                    for(var i = 0; i < model.Ground.Length; i++)
                                                        newCoords[i] = AdditionalMath.RotateAroundCenter(allCoords[i], center, AdditionalMath.ToRadians(block.Direction));

                                                    newMin = new Vec3(newCoords.Select(c => c.X).Min(), newCoords.Select(c => c.Y).Min(), newCoords.Select(c => c.Z).Min());
                                                }

                                                foreach (var unit in newCoords)
                                                    if (dirtBlock.Coord + offset == block.Coord + (Int3)(unit - newMin))
                                                        return false;
                                            }
                                            else if (dirtBlock.Coord + offset == block.Coord)
                                                return false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }).ToList();

            map.AnchoredObjects = map.AnchoredObjects.Where(x =>
            {
                var offset = default(Int3);

                foreach (var block in map.Blocks)
                {
                    if (parameters.Definitions.TryGetValue(block.Name, out Conversion[] variants))
                    {
                        if (variants != null)
                        {
                            var variant = block.Variant.GetValueOrDefault();

                            if (variants.Length > variant)
                            {
                                var conversion = variants[variant];

                                if (conversion != null) // If the variant actually has a conversion
                                {
                                    if (conversion.RemoveGround)
                                    {
                                        if (BlockInfoManager.BlockModels.TryGetValue(block.Name, out BlockModel model))
                                        {
                                            var center = default(Vec3);
                                            var allCoords = new Int3[model.Ground.Length];
                                            var newCoords = new Vec3[model.Ground.Length];
                                            var newMin = default(Vec3);

                                            if (model.Ground.Length > 1)
                                            {
                                                allCoords = model.Ground.Select(b => (Int3)b.Coord).ToArray();
                                                var min = new Int3(allCoords.Select(c => c.X).Min(), allCoords.Select(c => c.Y).Min(), allCoords.Select(c => c.Z).Min());
                                                var max = new Int3(allCoords.Select(c => c.X).Max(), allCoords.Select(c => c.Y).Max(), allCoords.Select(c => c.Z).Max());
                                                var size = max - min + (1, 1, 1);
                                                center = (min + max) * .5f;

                                                for (var i = 0; i < model.Ground.Length; i++)
                                                    newCoords[i] = AdditionalMath.RotateAroundCenter(allCoords[i], center, AdditionalMath.ToRadians(block.Direction));

                                                newMin = new Vec3(newCoords.Select(c => c.X).Min(), newCoords.Select(c => c.Y).Min(), newCoords.Select(c => c.Z).Min());
                                            }

                                            foreach (var unit in newCoords)
                                                if ((block.Coord + (1, 1, 1) + (Int3)(unit - newMin)) * map.Collection.GetBlockSize() == x.AbsolutePositionInMap)
                                                    return false;
                                        }
                                        else if (block.Coord + (1, 1, 1) * map.Collection.GetBlockSize() == x.AbsolutePositionInMap)
                                            return false;
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }).ToList();
        }

        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
