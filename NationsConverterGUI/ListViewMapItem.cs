using GBX.NET;
using GBX.NET.Engines.Game;
using NationsConverter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NationsConverterGUI
{
    public class ListViewMapItem
    {
        public string FileName => Path.GetFileName(GBX?.FileName);
        public BitmapImage Thumbnail { get; set; }
        public GameBox<CGameCtnChallenge> GBX { get; set; }
        public CGameCtnChallenge Map => GBX.MainNode;
        public SortedDictionary<string, SheetBlock> SheetBlocks { get; }
        public bool Updated { get; set; }

        public ListViewMapItem(GameBox<CGameCtnChallenge> gbx)
        {
            GBX = gbx;

            Thumbnail = new BitmapImage();
            SheetBlocks = new SortedDictionary<string, SheetBlock>();

            using (var ms = new MemoryStream())
            {
                if (gbx.MainNode.Thumbnail != null)
                {
                    gbx.MainNode.Thumbnail.Result.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

                    Thumbnail.BeginInit();
                    Thumbnail.StreamSource = ms;
                    Thumbnail.CacheOption = BitmapCacheOption.OnLoad;
                    Thumbnail.DecodePixelWidth = 128;
                    Thumbnail.EndInit();
                }
            }
        }
    }
}
