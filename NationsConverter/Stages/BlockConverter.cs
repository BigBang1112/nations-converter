using GBX.NET;
using GBX.NET.BlockInfo;
using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NationsConverter.Stages
{
    public class BlockConverter : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters, ConverterTemporary temporary)
        {
            var random = new Random(map.MapUid.GetHashCode());

            var skins = YamlManager.Parse<Dictionary<string, SkinDefinition>>("Skins.yml");

            var macroblocks = new Dictionary<string, CGameCtnMacroBlockInfo>();
            var log = new HashSet<string>();

            var blocks = map.Blocks.ToList();
            map.Blocks.Clear();

            foreach (var block in blocks)
            {
                if (parameters.Definitions.TryGetValue(block.Name, out Conversion[] variants)) // If the block has a definition in the sheet, return the possible variants
                {
                    if (variants != null)
                    {
                        var variant = block.Variant.GetValueOrDefault(); // Get the variant number

                        if (variants.Length > variant) // If the variant is available in the possible variants
                        {
                            var conversion = variants[variant]; // Reference it with 'conversion'

                            if (conversion != null) // If the variant actually has a conversion
                            {
                                ProcessConversion(block, conversion);
                            }
                            else
                                log.Add($"Missing but defined block variant: {block.Name} variant {block.Variant}");
                        }
                        else
                            log.Add($"Missing block variant: {block.Name} variant {block.Variant}");
                    }
                    else
                        log.Add($"Known block but undefined variants: {block.Name} variant {block.Variant}");
                }
                else
                    log.Add($"Missing block: {block.Name}");
            }

            /*void PlaceMacroblock(CGameCtnBlock referenceBlock, CGameCtnMacroBlockInfo macroblock)
            {
                foreach(var block in macroblock.Blocks)
                {
                    var b = block.Copy();
                    b.Coord += referenceBlock.Coord;

                    map.Blocks.Add(b);
                }
            }*/

            void ProcessConversion(CGameCtnBlock referenceBlock, Conversion conversion)
            {
                var radians = ((int)referenceBlock.Direction + conversion.OffsetDir) % 4 * (float)(Math.PI / 2);

                if (conversion.Block != null) // If a Block property is defined
                    ConvertBlock(referenceBlock, conversion, conversion.Block);

                if (conversion.Blocks != null) // If a Blocks property is defined
                {
                    var center = new Vec3();

                    if (conversion.OffsetCoordByBlockModel)
                    {
                        var allCoords = conversion.Blocks.Select(x => new Int3(x.OffsetCoord[0], x.OffsetCoord[1], x.OffsetCoord[2])).ToArray();
                        var min = new Int3(allCoords.Select(x => x.X).Min(), allCoords.Select(x => x.Y).Min(), allCoords.Select(x => x.Z).Min());
                        var max = new Int3(allCoords.Select(x => x.X).Max(), allCoords.Select(x => x.Y).Max(), allCoords.Select(x => x.Z).Max());
                        var size = max - min + (1, 1, 1);

                        center = (min + max) * .5f;

                        if (conversion.Center != null)
                            center = (Vec3)conversion.Center;

                        var newCoords = new List<Vec3>();

                        foreach (var c in conversion.Blocks)
                            newCoords.Add(AdditionalMath.RotateAroundCenter((Int3)c.OffsetCoord, center, radians));

                        if (center != default)
                        {
                            var newMin = new Vec3(newCoords.Select(x => x.X).Min(), newCoords.Select(x => x.Y).Min(), newCoords.Select(x => x.Z).Min());
                            newCoords = newCoords.Select(x => x - newMin).ToList();
                        }

                        for (var i = 0; i < conversion.Blocks.Length; i++)
                        {
                            var c = conversion.Blocks[i];

                            ConvertBlock(referenceBlock, conversion, c, referenceBlock.Coord + (Int3)newCoords[i]);
                        }
                    }
                    else
                    {
                        foreach (var c in conversion.Blocks)
                        {
                            ConvertBlock(referenceBlock, conversion, c, referenceBlock.Coord + (Int3)c.OffsetCoord);
                        }
                    }
                }

                if (conversion.Item != null)
                    PlaceItem(conversion.Item);

                if (conversion.Items != null)
                    foreach (var item in conversion.Items)
                        PlaceItem(item);

                if (conversion.Light != null)
                    PlaceLight(conversion.Light);

                if (conversion.Lights != null)
                    foreach (var light in conversion.Lights)
                        PlaceLight(light);

                void PlaceItem(ConversionItem conversionItem)
                {
                    if (conversionItem == null) return;

                    PlaceObject(conversionItem.Name,
                        (Vec3)conversionItem.OffsetPos,
                        (Vec3)conversionItem.OffsetPos2,
                        (Vec3)conversionItem.OffsetPivot,
                        (Vec3)conversionItem.OffsetRot);
                }

                void PlaceLight(ConversionLight conversionLight)
                {
                    if (conversionLight == null) return;

                    var color = conversionLight.Color;
                    if (parameters.ChristmasMode)
                        color = LightManager.ChirstmasLights[random.Next(LightManager.ChirstmasLights.Length)].ToString("X3");

                    PlaceObject($"NationsConverter\\Lights\\Light_{color}.Item.Gbx", default, default, (Vec3)conversionLight.Offset, default);
                }

                void PlaceObject(string convName, Vec3 convOffsetPos, Vec3 convOffsetPos2, Vec3 convOffsetPivot, Vec3 convOffsetRot)
                {
                    var offsetPos = convOffsetPos;

                    var center = new Vec3(0, 0, 0);
                    offsetPos = AdditionalMath.RotateAroundCenter(offsetPos, center, radians);

                    if (version <= GameVersion.TMUF)
                    {
                        offsetPos -= parameters.Stadium2RelativeOffset.XZ;
                        offsetPos += convOffsetPos2;
                    }

                    var name = "";
                    var collection = 26;
                    var author = "Nadeo";
                    if (convName == null) throw new Exception();
                    var meta = convName.Split(' ');
                    if (meta.Length == 0) throw new Exception();
                    name = meta[0];
                    if (meta.Length > 1)
                    {
                        int.TryParse(meta[1], out collection);
                        if(meta.Length == 3) author = meta[2];
                    }
                    if (name.StartsWith("NationsConverter"))
                        author = "Nations Converter Team";

                    var offsetPivot = convOffsetPivot;

                    if(conversion.OffsetPivotByBlockModel)
                    {
                        IEnumerable<Int3> allCoords = null;
                        if(referenceBlock.IsGround && BlockInfoManager.BlockModels[referenceBlock.Name].Ground != null)
                            allCoords = BlockInfoManager.BlockModels[referenceBlock.Name].Ground.Select(x => (Int3)x.Coord);
                        else if(BlockInfoManager.BlockModels[referenceBlock.Name].Air != null)
                            allCoords = BlockInfoManager.BlockModels[referenceBlock.Name].Air.Select(x => (Int3)x.Coord);

                        var min = new Int3(allCoords.Select(x => x.X).Min(), allCoords.Select(x => x.Y).Min(), allCoords.Select(x => x.Z).Min());
                        var max = new Int3(allCoords.Select(x => x.X).Max(), allCoords.Select(x => x.Y).Max(), allCoords.Select(x => x.Z).Max());
                        var box = max - min;

                        var directions = new Int3[]
                        {
                            (0, 0, 0),
                            (0, 0, box.Z),
                            (box.X, 0, box.Z),
                            (box.X, 0, 0)
                        };

                        offsetPivot += directions[(int)referenceBlock.Direction] * (32, 8, 32);
                    }

                    map.PlaceAnchoredObject(
                        new Ident(name, collection, author),
                        referenceBlock.Coord * new Vec3(32, 8, 32) + offsetPos + (16, 8, 16),
                        (-radians, 0, 0) - AdditionalMath.ToRadians(convOffsetRot),
                        -offsetPivot);
                }

                if (conversion.Macroblock != null)
                {
                    if (!macroblocks.TryGetValue(conversion.Macroblock, out CGameCtnMacroBlockInfo macro))
                        macro = macroblocks[conversion.Macroblock] = GameBox.Parse<CGameCtnMacroBlockInfo>($"{Converter.LocalDirectory}/Macroblocks/{conversion.Macroblock}").MainNode;
                    map.PlaceMacroblock(macro, referenceBlock.Coord + (0, conversion.OffsetY, 0), Dir.Add(referenceBlock.Direction, (Direction)conversion.OffsetDir));
                }

                if (conversion.FreeBlock != null)
                    PlaceFreeBlock(conversion.FreeBlock);

                if (conversion.FreeBlocks != null)
                    foreach(var block in conversion.FreeBlocks)
                        PlaceFreeBlock(block);

                void PlaceFreeBlock(ConversionFreeBlock block)
                {
                    var offsetPos = (Vec3)block.OffsetPos;
                    var offsetRot = AdditionalMath.ToRadians((Vec3)block.OffsetRot); // Gets the offset rotation in radians

                    offsetPos += AdditionalMath.RotateAroundCenter((0, 0, 0), (16, 0, 16), offsetRot.X);
                    // Makes the offset rotation affect around center of the single piece of the block

                    offsetPos = AdditionalMath.RotateAroundCenter(offsetPos, (16, 0, 16), radians);
                    // Applies the block rotation from the center of the entire block structure

                    if (version <= GameVersion.TMUF)
                        offsetPos -= parameters.Stadium2RelativeOffset * (1, 0, 1);

                    CGameCtnBlockSkin skin = null;
                    if (block.Skin != null)
                    {
                        skin = new CGameCtnBlockSkin()
                        {
                            PackDesc = new FileRef(3, FileRef.DefaultChecksum, "Skins\\" + block.Skin, "")
                        };

                        skin.CreateChunk<CGameCtnBlockSkin.Chunk03059002>();
                        skin.CreateChunk<CGameCtnBlockSkin.Chunk03059003>();
                    }

                    var freeBlock = map.PlaceFreeBlock(block.Name,
                        (referenceBlock.Coord + (0, 1, 0)) * (32, 8, 32) + offsetPos,
                        (-radians, 0, 0) - offsetRot, skin);
                    if (skin != null)
                        freeBlock.Author = "Nadeo";
                }

                if (referenceBlock.IsGround)
                {
                    if(conversion.Ground != null)
                        ProcessConversion(referenceBlock, conversion.Ground);

                    var blockModelExists = BlockInfoManager.BlockModels.TryGetValue(referenceBlock.Name, out BlockModel blockModel);

                    if (conversion.DirtGround != null && temporary.DirtCoords.Exists(x =>
                    {
                        if(blockModelExists)
                        {
                            if(blockModel.Ground.Length > 1)
                            {
                                foreach(var unit in blockModel.Ground)
                                {
                                    if (x.XZ == referenceBlock.Coord.XZ + ((Int3)unit.Coord).XZ)
                                        return true;
                                }
                            }
                        }
                        return x.XZ == referenceBlock.Coord.XZ;
                    }))
                        ProcessConversion(referenceBlock, conversion.DirtGround);
                    else if (conversion.FabricGround != null && blocks.Exists(x => x.Coord.XZ == referenceBlock.Coord.XZ && x.Name == "StadiumFabricCross1x1"))
                        ProcessConversion(referenceBlock, conversion.FabricGround);
                    else if (conversion.GrassGround != null)
                        ProcessConversion(referenceBlock, conversion.GrassGround);
                }
                else if (conversion.Air != null)
                    ProcessConversion(referenceBlock, conversion.Air);
            }

            void ConvertBlock(CGameCtnBlock referenceBlock, Conversion conversion, ConversionBlock conversionBlock,
                Int3? newCoord = null)
            {
                var block = new CGameCtnBlock(conversionBlock.Name,
                    referenceBlock.Direction,
                    referenceBlock.Coord,
                    0);

                if (newCoord.HasValue)
                    block.Coord = newCoord.Value;
                else
                {
                    if (conversionBlock.OffsetCoord2 != null && version >= GameVersion.TM2)
                    {
                        if (conversionBlock.OffsetCoord2.Length >= 3)
                            block.Coord += (Int3)conversionBlock.OffsetCoord2;
                    }
                    else if (conversionBlock.OffsetCoord != null)
                        if (conversionBlock.OffsetCoord.Length >= 3)
                            block.Coord += (Int3)conversionBlock.OffsetCoord;
                }

                block.Coord += (0, conversionBlock.OffsetY, 0);

                if (conversion.OffsetDir != 0)
                    block.Direction = (Direction)(((int)block.Direction + conversion.OffsetDir) % 4);
                if (conversionBlock.OffsetDir != 0)
                    block.Direction = (Direction)(((int)block.Direction + conversionBlock.OffsetDir) % 4);

                var direction = (int)block.Direction;
                var radians = direction * (float)(Math.PI / 2);

                if (conversionBlock.Ghost.HasValue)
                    block.IsGhost = conversionBlock.Ghost.Value;

                if (conversionBlock.Variant.HasValue)
                    block.Variant = conversionBlock.Variant.Value;

                if (conversionBlock.Bit17.HasValue)
                    block.Bit17 = conversionBlock.Bit17.Value;

                if (conversionBlock.Bit21.HasValue)
                    block.Bit21 = conversionBlock.Bit21.Value;

                if(conversionBlock.Skinnable)
                {
                    if(referenceBlock.Skin != null)
                    {
                        SkinDefinition def = null;
                        if (!string.IsNullOrEmpty(referenceBlock.Skin.PackDesc.LocatorUrl) || skins.TryGetValue(referenceBlock.Skin.PackDesc.FilePath.Substring("Skins\\".Length), out def))
                        {
                            var skin = new CGameCtnBlockSkin();
                            skin.Text = "!4";
                            skin.CreateChunk<CGameCtnBlockSkin.Chunk03059002>();
                            skin.CreateChunk<CGameCtnBlockSkin.Chunk03059003>();

                            if (!string.IsNullOrEmpty(referenceBlock.Skin.PackDesc.LocatorUrl))
                            {
                                skin.PackDesc.FilePath = $"Skins\\Any\\{Path.GetFileName(referenceBlock.Skin.PackDesc.LocatorUrl)}";
                                skin.PackDesc.LocatorUrl = referenceBlock.Skin.PackDesc.LocatorUrl;
                            }
                            else
                            {
                                if (def.Primary != null) skin.PackDesc.FilePath = $"Skins\\{def.Primary}";
                                if (def.Secondary != null) skin.SecondaryPackDesc.FilePath = $"Skins\\{def.Secondary}";
                            }

                            block.Skin = skin;
                            block.Author = "Nadeo";
                        }
                    }
                }

                if (referenceBlock.WaypointSpecialProperty != null)
                    block.WaypointSpecialProperty = referenceBlock.WaypointSpecialProperty;

                block.IsGround = false;

                if (conversionBlock.Flags.HasValue)
                    block.Flags = conversionBlock.Flags.Value;

                map.Blocks.Add(block);
            }

            var sortedLog = log.ToList();
            sortedLog.Sort();
        }

        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
