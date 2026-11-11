using Arthur_Showdown_Shared.Dtos.Game;

namespace Arthur_Showdown_Game.Components.Game
{
    public class CartaAtaque
    {
        public AtaqueDto Ataque { get; set; } = default!;
        public int DeckAtaquesId { get; set; }
        public int CooldownRestante { get; set; }
        public int? PosicaoFila { get; set; }
        public int QtdUsosNaRun { get; set; }
        public int QtdVezesAdicionadoFila { get; set; }
        public int QtdVezesRemovidoFila { get; set; }
        public bool EmFila => PosicaoFila.HasValue;
        public bool EmCooldown => CooldownRestante > 0;
    }
}
