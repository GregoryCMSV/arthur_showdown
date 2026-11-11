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
        public int TamanhoMapa { get; set; } = 5;
        public int SessaoNumero { get; set; }
        public int TotalSessoes { get; set; }
        public int WaveNumero { get; set; }
        public int TotalWaves { get; set; }
        public bool Vitoria { get; set; }
        public List<CartaEstadoDto> Deck { get; set; } = new();
        public List<InimigoEstadoDto> Inimigos { get; set; } = new();
    }
}
