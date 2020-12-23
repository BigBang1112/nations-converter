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
            map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Items/NationsConverter/z_terrain/u_blue/BlueGround.Item.Gbx", "Items/NationsConverter/z_terrain/u_blue");
            map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Items/NationsConverter/z_terrain/w_grass/GrassGround.Item.Gbx", "Items/NationsConverter/z_terrain/w_grass");
            map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Items/NationsConverter/z_terrain/w_grass/GrassEdgeStraight2.Item.Gbx", "Items/NationsConverter/z_terrain/w_grass");
            map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Items/NationsConverter/z_terrain/w_grass/GrassEdgeCornerIn2.Item.Gbx", "Items/NationsConverter/z_terrain/w_grass");

            map.PlaceAnchoredObject(
               (@"NationsConverter\z_terrain\w_grass\GrassEdgeCornerIn2.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
               (7, 1, 7) * map.Collection.GetBlockSize(),
               (0, 0, 0));
            map.PlaceAnchoredObject(
               (@"NationsConverter\z_terrain\w_grass\GrassEdgeCornerIn2.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
               (7, 1, 41) * map.Collection.GetBlockSize(),
               ((float)Math.PI/2, 0, 0));
            map.PlaceAnchoredObject(
               (@"NationsConverter\z_terrain\w_grass\GrassEdgeCornerIn2.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
               (41, 1, 41) * map.Collection.GetBlockSize(),
               ((float)Math.PI, 0, 0));
            map.PlaceAnchoredObject(
               (@"NationsConverter\z_terrain\w_grass\GrassEdgeCornerIn2.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
               (41, 1, 7) * map.Collection.GetBlockSize(),
               (-(float)Math.PI / 2, 0, 0));

            for (var d = 0; d < 4; d++)
            {
                for (var i = 0; i < 32; i++)
                {
                    if (d == 0)
                        map.PlaceAnchoredObject(
                           (@"NationsConverter\z_terrain\w_grass\GrassEdgeStraight2.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
                           (7, 1, i + 9) * map.Collection.GetBlockSize(),
                           ((float)Math.PI / 2, 0, 0));
                    if (d == 1)
                        map.PlaceAnchoredObject(
                           (@"NationsConverter\z_terrain\w_grass\GrassEdgeStraight2.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
                           (i + 9, 1, 41) * map.Collection.GetBlockSize(),
                           ((float)Math.PI, 0, 0));
                    if (d == 2)
                        map.PlaceAnchoredObject(
                           (@"NationsConverter\z_terrain\w_grass\GrassEdgeStraight2.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
                           (41, 1, 39 - i) * map.Collection.GetBlockSize(),
                           (-(float)Math.PI / 2, 0, 0));
                    if (d == 3)
                        map.PlaceAnchoredObject(
                           (@"NationsConverter\z_terrain\w_grass\GrassEdgeStraight2.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
                           (39 - i, 1, 7) * map.Collection.GetBlockSize(),
                           (0, 0, 0));
                }
            }

            var dirtBlocks = new string[] { "StadiumDirt", "StadiumDirtHill", "StadiumPool", "StadiumWater", "StadiumPool2", "StadiumWater2" };

            var grassCoords = new List<Int3>();

            for (var x = 0+8; x < 32+8; x++)
            {
                for (var z = 0 + 8; z < 32 + 8; z++)
                {
                    var dirtBlockExists = false;

                    Int3 coord = (x, 0, z);
                    if (version <= GameVersion.TMUF)
                        coord -= (1, 0, 1);

                    foreach (var groundBlock in map.Blocks.Where(o => o.Coord == (x, 0, z)
                    || (version <= GameVersion.TMUF &&
                       (o.Coord == coord + (0, 1, 0) // TMNF hill
                    ||  o.Coord == coord + (0, -1, 0))) // TMNF base

                    || (version >= GameVersion.TM2 && (o.Name == "StadiumPool2" || o.Name == "StadiumWater2") && o.Coord == (x, -1, z)))) 
                    {
                        if (dirtBlocks.Contains(groundBlock.Name))
                        {
                            dirtBlockExists = true;
                            break;
                        }
                    }

                    var fabricExists = map.Blocks.Where(o => o.Coord.XZ == coord && o.Name == "StadiumFabricCross1x1").Count() > 0;

                    if (!dirtBlockExists)
                    {
                        grassCoords.Add((x, 1, z));

                        if (fabricExists)
                        {
                            grassCoords.Add((x, 1, z));

                            map.PlaceAnchoredObject(
                                (@"NationsConverter\z_terrain\u_blue\BlueGround.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
                                (x, 1, z) * map.Collection.GetBlockSize(),
                                (0, 0, 0));
                        }
                        else
                        {
                            map.PlaceAnchoredObject(
                                (@"NationsConverter\z_terrain\w_grass\GrassGround.Item.Gbx", new Collection(26), "pTuyJG9STcCN_11BiU3t0Q"),
                                (x, 1, z) * map.Collection.GetBlockSize(),
                                (0, 0, 0));
                        }
                    }
                }
            }

            var blocks = map.Blocks.ToArray();

            map.Blocks = blocks.Where(x =>
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
                                        bool DoRemoveGround(Conversion conv)
                                        {
                                            if (conv.RemoveGround)
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
                                                        if (dirtBlock.Coord + offset == block.Coord + (Int3)(unit - newMin))
                                                            return false;
                                                }
                                                else if (dirtBlock.Coord + offset == block.Coord)
                                                    return false;
                                            }

                                            return true;
                                        }

                                        if (!DoRemoveGround(conversion)) return false;

                                        if (block.IsGround && conversion.Ground != null)
                                            DoRemoveGround(conversion.Ground);
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }).ToList();
            
            var itemOffset = default(Vec3);
            if (version >= GameVersion.TM2)
                itemOffset = parameters.Stadium2RelativeOffset * (1, 0, 1);

            map.AnchoredObjects = map.AnchoredObjects.Where(x =>
            {
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
                                    bool DoRemoveGround(Conversion conv)
                                    {
                                        if (conv.RemoveGround)
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
                                                    if ((block.Coord + (1, 1, 1) + (Int3)(unit - newMin)) * map.Collection.GetBlockSize() == x.AbsolutePositionInMap - itemOffset)
                                                        return false;
                                            }
                                            else if ((block.Coord + (1, 1, 1)) * map.Collection.GetBlockSize() == x.AbsolutePositionInMap - itemOffset)
                                                return false;
                                        }

                                        return true;
                                    }

                                    if (block.IsGround && conversion.Ground != null)
                                        if (!DoRemoveGround(conversion.Ground))
                                            return false;

                                    if (!DoRemoveGround(conversion)) return false;
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
