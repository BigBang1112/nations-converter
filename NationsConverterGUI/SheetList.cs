using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NationsConverterGUI
{
    public class SheetList : List<ConversionView>
    {
        public string SheetName { get; set; }
        public string BlockName { get; set; }

        public SheetList(string sheetName, IEnumerable<ConversionView> conversions) : base(conversions)
        {
            SheetName = sheetName;
        }
    }
}
