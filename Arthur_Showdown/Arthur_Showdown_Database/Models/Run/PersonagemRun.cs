using Arthur_Showdown_Database.Models.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Run
{
    public class PersonagemRun
    {
        public int Id { get; set; }
        public int PersonagemId { get; set; }
        public  Personagem? Personagem { get; set; }
        public int VidaMaxima { get; set; }
        public int VidaAtual { get; set; }
        public int GameId { get; set; }
        public Game? Game { get; set; }
    }
}
