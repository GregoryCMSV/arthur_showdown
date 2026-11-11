using System.Collections.Generic;

namespace Arthur_Showdown_Shared.Dtos.Game
{
    public enum TipoMelhoriaAtaque
    {
        MaisUmDano = 1,
        MaisDoisDanoMaisUmCooldown = 2,
        MenosUmCooldown = 3,
        AcaoRapida = 4
    }

    public class AplicarMelhoriaRequest
    {
        public int DeckAtaquesId { get; set; }
        public TipoMelhoriaAtaque Tipo { get; set; }
    }

    public class EscolherNovoAtaqueRequest
    {
        public int AtaqueId { get; set; }
    }

    public class WavePlanejadaDto
    {
        public int WaveNumero { get; set; }
        public List<InimigoEstadoDto> Inimigos { get; set; } = new();
        public TipoMelhoriaAtaque? MelhoriaAposWave { get; set; }
    }

    public class SessaoPlanejadaDto
    {
        public int SessaoNumero { get; set; }
        public int TamanhoMapa { get; set; } = 5;
        public List<WavePlanejadaDto> Waves { get; set; } = new();
        public List<AtaqueDto> OpcoesNovoAtaqueAposSessao { get; set; } = new();
    }

    public class EstadoJogoDto
    {
        public int GameId { get; set; }
        public int OuroAtual { get; set; }
        public int TotalOuroGanho { get; set; }
        public int LocalAtualId { get; set; }
        public int Turno { get; set; }
        public int TempoSegundos { get; set; }
        public int QtdMudancaDirecao { get; set; }
        public int QtdHitsSofridos { get; set; }
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
        public List<SessaoPlanejadaDto> PlanoSessoes { get; set; } = new();
    }
}
