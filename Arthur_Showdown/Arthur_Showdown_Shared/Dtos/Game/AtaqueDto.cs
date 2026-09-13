using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class AtaqueDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string HitboxJson { get; set; } = string.Empty;
        public int Dano { get; set; }
        public int Cooldown { get; set; }
        public bool IsFastAction { get; set; }
        public int LimiteAprimoramento { get; set; }
    }
}
