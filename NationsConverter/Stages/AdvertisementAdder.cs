using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NationsConverter.Stages
{
    public class AdvertisementAdder : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            foreach(var block in map.Blocks)
            {
                if(block.Name == "RoadTechStart" || block.Name == "RoadTechFinish" || block.Name == "RoadTechMultilap")
                {
                    var skin = new CGameCtnBlockSkin();
                    skin.Text = "!4";
                    skin.CreateChunk<CGameCtnBlockSkin.Chunk03059002>();

                    var ncImgUrl = "http://bigbang1112.eu/nc/NCStartBlock1.png";
                    skin.PackDesc.FilePath = $"Skins\\Any\\{Path.GetFileName(ncImgUrl)}";
                    skin.PackDesc.LocatorUrl = ncImgUrl;

                    block.Skin = skin;
                    block.Author = "Nadeo";
                }
            }
        }
    }
}
