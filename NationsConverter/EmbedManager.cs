using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NationsConverter
{
    public class EmbedManager
    {
        public void CopyUsedEmbed(CGameCtnChallenge map, Definitions definitions)
        {
            var previousEmbed = map.Embeds; // TODO: maybe later some kind of embed transfer support

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
                                if (conversion.Block != null)
                                    ImportBlock(conversion.Block);

                                if (conversion.Blocks != null)
                                    foreach (var conversionBlock in conversion.Blocks)
                                        ImportBlock(conversionBlock);

                                void ImportBlock(ConversionBlock conversionBlock)
                                {
                                    try
                                    {
                                        if (conversionBlock.Name.EndsWith(".Block.Gbx_CustomBlock"))
                                            map.ImportFileToEmbed($"UserData/Blocks/{conversionBlock.Name}", $"Blocks/{Path.GetDirectoryName(conversionBlock.Name)}");
                                    }
                                    catch (Exception e) when (e is DirectoryNotFoundException || e is FileNotFoundException)
                                    {

                                    }
                                }

                                if (conversion.Item != null)
                                    ImportItem(conversion.Item);

                                if (conversion.Items != null)
                                    foreach (var conversionItem in conversion.Items)
                                        ImportItem(conversionItem);

                                void ImportItem(ConversionItem conversionItem)
                                {
                                    try
                                    {
                                        var itemName = conversionItem.Name.Split(' ');
                                        if (itemName.Length >= 3 && itemName[2] != "Nadeo")
                                            map.ImportFileToEmbed($"UserData/Items/{itemName[0]}", $"Items/{Path.GetDirectoryName(itemName[0])}");
                                    }
                                    catch (Exception e) when (e is DirectoryNotFoundException || e is FileNotFoundException)
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
