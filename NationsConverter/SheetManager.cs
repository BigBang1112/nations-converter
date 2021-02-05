using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NationsConverter
{
    public class SheetManager
    {
        public Sheet StockSheet { get; }
        public List<Sheet> AdditionalSheets { get; set; }

        public Definitions Definitions { get; private set; }
        public Dictionary<string, ItemSkinPack[]> ItemSkinPacks { get; set; }

        public SheetManager(Sheet stockSheet, params Sheet[] sheets)
        {
            StockSheet = stockSheet;
            AdditionalSheets = sheets.ToList();
            Definitions = new Definitions();
            ItemSkinPacks = stockSheet.ItemSkinPacks;

            foreach (var sheet in sheets)
            {
                foreach(var pair in sheet.ItemSkinPacks)
                {
                    ItemSkinPacks[pair.Key] = pair.Value;
                }
            }
        }

        public void UpdateDefinitions()
        {
            var definitions = StockSheet.Blocks;

            foreach (var s in AdditionalSheets) // Merge block sheets, upper is prioritized
            {
                foreach (var def in s.Blocks)
                {
                    if (definitions.TryGetValue(def.Key, out Conversion[] conversions))
                    {
                        if (conversions == null) // If the sheet adds brand new conversion possibility
                            definitions[def.Key] = def.Value;
                        else if (def.Value != null) // If previous sheet did contain some convertions related to this block and the new stuff isn't null
                        {
                            for (var i = 0; i < def.Value.Length; i++) // For each new conversion
                            {
                                if (definitions[def.Key].Length <= i) // If new conversions overcount the current conversions array
                                {
                                    var bd = definitions[def.Key];
                                    Array.Resize(ref bd, def.Value.Length); // Resize the array to at least the amount of new conversions definitions
                                    definitions[def.Key] = bd;
                                }

                                definitions[def.Key][i] = def.Value[i];
                            }
                        }
                    }
                    else
                        definitions[def.Key] = def.Value;
                }
            }

            Definitions = definitions;
        }
    }
}
