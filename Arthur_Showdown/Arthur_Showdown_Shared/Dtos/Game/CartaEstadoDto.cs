namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class CartaEstadoDto
    {
        public int DeckAtaquesId { get; set; }
        public int CooldownRestante { get; set; }
        public int? PosicaoFila { get; set; }
        public int QtdUsosNaRun { get; set; }
        public int QtdVezesAdicionadoFila { get; set; }
        public int QtdVezesRemovidoFila { get; set; }
        public int? EfeitoId { get; set; }
        public AtaqueDto? Ataque { get; set; }
    }
}
