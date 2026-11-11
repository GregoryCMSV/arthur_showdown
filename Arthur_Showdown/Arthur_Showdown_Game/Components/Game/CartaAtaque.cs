using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public class CartaAtaque
    {
        public AtaqueDto Ataque { get; init; } = default!;
        public int DeckAtaquesId { get; set; }
        public int CooldownRestante { get; set; }
        public int? PosicaoFila { get; set; }
        public bool EmFila => PosicaoFila.HasValue;
        public bool EmCooldown => CooldownRestante > 0;
    }
}
