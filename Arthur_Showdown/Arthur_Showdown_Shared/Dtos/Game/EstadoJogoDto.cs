using System.Collections.Generic;

namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class EstadoJogoDto
    {
        public int GameId { get; set; }
        public int OuroAtual { get; set; }
        public int LocalAtualId { get; set; }
        public int Turno { get; set; }
        public int VidaAtual { get; set; }
        public int VidaMaxima { get; set; }
        public int Posicao { get; set; }
        public int DirecaoOlhar { get; set; } = 1;
        public List<CartaEstadoDto> Deck { get; set; } = new();
        public List<InimigoEstadoDto> Inimigos { get; set; } = new();
    }
}
