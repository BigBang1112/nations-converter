using GBX.NET.Engines.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationsConverter.Stages
{
    public interface IStage
    {
        void Process(CGameCtnChallenge map, int version, ConverterParameters parameters);
    }
}
