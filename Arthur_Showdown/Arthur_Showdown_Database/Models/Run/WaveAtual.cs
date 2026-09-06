using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Run
{
    public class WaveAtual
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game? Game { get; set; }
        public List<int> InimigosGeradosIds { get; set; } = new();
    }
}
