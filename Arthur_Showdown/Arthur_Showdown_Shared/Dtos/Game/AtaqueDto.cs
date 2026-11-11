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
        public int? EfeitoPadraoId { get; set; }
        public int? EfeitoAdicionalId { get; set; }

        public int? ObterEfeitoPadraoEfetivo()
        {
            if (EfeitoPadraoId.HasValue) return EfeitoPadraoId;
            if (Id == 2 || Nome.Equals("Escudo", StringComparison.OrdinalIgnoreCase)) return 3;
            if (Nome.Contains("Amaldiçoar", StringComparison.OrdinalIgnoreCase)) return 1;
            if (Nome.Contains("Espelho", StringComparison.OrdinalIgnoreCase)) return 2;
            return null;
        }

        public bool PossuiEfeito(int efeitoId)
            => ObterEfeitoPadraoEfetivo() == efeitoId || EfeitoAdicionalId == efeitoId;

        public static string NomeEfeito(int efeitoId) => efeitoId switch
        {
            1 => "Maldição",
            2 => "Espelho",
            3 => "Escudo",
            _ => ""
        };
    }
}
