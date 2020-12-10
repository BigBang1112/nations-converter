using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter.Stages
{
    public class MapUidChanger : IStage
    {
        public void Process(CGameCtnChallenge map, int version, ConverterParameters parameters)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
            var stringChars = new char[27];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            //map.MapUid = new string(stringChars);
        }
    }
}
