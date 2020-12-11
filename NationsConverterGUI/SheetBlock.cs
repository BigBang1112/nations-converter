using NationsConverter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NationsConverterGUI
{
    public class SheetBlock
    {
        public string BlockName { get; set; }
        public int SelectedSheet { get; set; }
        public string[] Sheets { get; set; }
        public Dictionary<int, SheetList> Conversions { get; set; }
        public List<BitmapImage> Icons { get; set; }
    }
}
