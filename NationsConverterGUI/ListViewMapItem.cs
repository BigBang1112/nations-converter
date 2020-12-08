using GBX.NET;
using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
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

        public ListViewMapItem(GameBox<CGameCtnChallenge> gbx)
        {
            GBX = gbx;

            Thumbnail = new BitmapImage();

            using (var ms = new MemoryStream())
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
