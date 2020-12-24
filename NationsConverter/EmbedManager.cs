using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace NationsConverter
{
    public class EmbedManager
    {
        public void CopyUsedEmbed(CGameCtnChallenge map, Definitions definitions, ConverterParameters parameters)
        {
            var previousEmbed = map.Embeds; // TODO: maybe later some kind of embed transfer support

            map.Embeds.Clear();
            map.RemoveChunk<CGameCtnChallenge.Chunk03043054>(); // TODO: Fix 0x054 properly for MP3 maps

            map.CreateChunk<CGameCtnChallenge.Chunk03043054>();

            var filesNotFound = new List<string>();

            foreach (var block in map.Blocks)
            {
                if (definitions.TryGetValue(block.Name, out Conversion[] conversions))
                {
                    if (conversions != null && block.Variant.HasValue)
                    {
                        if (conversions.Length > block.Variant.Value)
                        {
                            var conversion = conversions[block.Variant.Value];

                            if (conversion != null)
                            {
                                Import(conversion);

                                void Import(Conversion c)
                                {
                                    if (c.Block != null)
                                        ImportBlock(c.Block);

                                    if (c.Blocks != null)
                                        foreach (var conversionBlock in c.Blocks)
                                            ImportBlock(conversionBlock);

                                    void ImportBlock(ConversionBlock conversionBlock)
                                    {
                                        if (conversionBlock == null) return;

                                        try
                                        {
                                            if (conversionBlock.Name.EndsWith(".Block.Gbx_CustomBlock"))
                                                map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Blocks/{conversionBlock.Name}", $"Blocks/{Path.GetDirectoryName(conversionBlock.Name)}");
                                        }
                                        catch (Exception e) when (e is DirectoryNotFoundException || e is FileNotFoundException)
                                        {

                                        }
                                    }

                                    if (c.Item != null)
                                        ImportItem(c.Item);

                                    if (c.Items != null)
                                        foreach (var conversionItem in c.Items)
                                            ImportItem(conversionItem);

                                    void ImportItem(ConversionItem conversionItem)
                                    {
                                        if (conversionItem == null) return;

                                        try
                                        {
                                            var itemName = conversionItem.Name.Split(' ');
                                            if (itemName.Length > 0 && itemName[0].StartsWith("NationsConverter"))
                                                map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Items/{itemName[0]}", $"Items/{Path.GetDirectoryName(itemName[0])}");
                                        }
                                        catch (Exception e) when (e is DirectoryNotFoundException || e is FileNotFoundException)
                                        {

                                        }
                                    }

                                    if (parameters.ChristmasMode)
                                    {
                                        foreach(var color in LightManager.ChirstmasLights)
                                        {
                                            map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Items/NationsConverter/Lights/Light_{color:X3}.Item.Gbx",
                                                $"Items/NationsConverter/Lights");
                                        }
                                    }
                                    else
                                    {
                                        if (c.Light != null)
                                            ImportLight(c.Light);

                                        if (c.Lights != null)
                                            foreach (var conversionLight in c.Lights)
                                                ImportLight(conversionLight);

                                        void ImportLight(ConversionLight conversionLight)
                                        {
                                            if (conversionLight == null) return;

                                            try
                                            {
                                                map.ImportFileToEmbed($"{Converter.LocalDirectory}/UserData/Items/NationsConverter/Lights/Light_{conversionLight.Color}.Item.Gbx",
                                                    $"Items/NationsConverter/Lights");
                                            }
                                            catch (Exception e) when (e is DirectoryNotFoundException || e is FileNotFoundException)
                                            {

                                            }
                                        }
                                    }

                                    if (c.Ground != null && block.IsGround)
                                        Import(c.Ground);

                                    if (c.Air != null && !block.IsGround)
                                        Import(c.Air);

                                    if(c.DirtGround != null && map.Blocks.Exists(x => x.Coord.XZ == block.Coord.XZ && x.Name == "StadiumDirt"))
                                        Import(c.DirtGround);
                                    else if(c.FabricGround != null && map.Blocks.Exists(x => x.Coord.XZ == block.Coord.XZ && x.Name == "StadiumFabricCross1x1"))
                                        Import(c.FabricGround);
                                    else if(c.GrassGround != null)
                                        Import(c.GrassGround);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
