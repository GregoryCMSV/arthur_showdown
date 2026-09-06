using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Base
{
    public class Local
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int OrdemProgressao { get; set; }
        public int QtdSessoes { get; set; }
        public int? ChefeId { get; set; }
        public Personagem? Chefe { get; set; }
        public List<int> InimigosPossiveisIds { get; set; } = new();
    }
}
