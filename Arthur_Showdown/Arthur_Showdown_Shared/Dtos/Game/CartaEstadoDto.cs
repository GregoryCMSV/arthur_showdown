namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class CartaEstadoDto
    {
        public int DeckAtaquesId { get; set; }
        public int CooldownRestante { get; set; }
        public int? PosicaoFila { get; set; }
        public AtaqueDto? Ataque { get; set; }
    }
}
