using Arthur_Showdown_Database.Models.Auxiliar;
using Arthur_Showdown_Database.Models.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Run
{
    public class Game
    {
        public int Id { get; set; }
        public Guid JogadorId { get; set; }
        public int TotalOuroGanho { get; set; }
        public int OuroAtual { get; set; }
        public int LocalAtualId { get; set; }
        public Local? LocalAtual { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public int StatusRunId { get; set; }
        public StatusRun? StatusRun { get; set; }
        public int TempoSegundos { get; set; }
        public int TurnosJogados { get; set; }
        public int QtdMudancaDirecao { get; set; }
        public int QtdHitsSofridos { get; set; }
        public int? SessaoAtualId { get; set; }
        public Sessao? SessaoAtual { get; set; }
        public int? WaveAtualId { get; set; }
        public WaveAtual? WaveAtual { get; set; }
    }
}
