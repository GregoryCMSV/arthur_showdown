using Arthur_Showdown_Database.Models.Run;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Base
{
    public class InimigoRun
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game? Game { get; set; }
        public int PersonagemId { get; set; }
        public Personagem? Personagem { get; set; }
        public int VidaAtual { get; set; }
        public int Posicao { get; set; }
        public int CursorAcao { get; set; }
        public int DirecaoOlhar { get; set; } = 1;
        public bool Preparado { get; set; }      // ataque ja esta na fila do inimigo
        public bool Telegrafando { get; set; }   // tile piscando: ataca no proximo turno
        public bool RecuoPendente { get; set; }
    }
}
