namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class CartaEstadoDto
    {
        public int DeckAtaquesId { get; set; }
        public int CooldownRestante { get; set; }
        public int? PosicaoFila { get; set; }   // posicao na fila de ataques do jogador (null = fora da fila)
        public AtaqueDto Ataque { get; set; } = default!;   // so e preenchido no GET
    }
}
