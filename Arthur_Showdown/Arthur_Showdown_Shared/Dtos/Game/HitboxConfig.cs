using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class HitboxConfig
    {
        public HitboxOrigem Origem { get; set; } = HitboxOrigem.Caster;
        public HitboxDirecao Direcao { get; set; } = HitboxDirecao.Forward;
        public int AlcanceMin { get; set; } = 1;
        public int AlcanceMax { get; set; } = 1;
    }
}
