using Arthur_Showdown_Database.Models.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Auxiliar
{
    public class TipoPersonagem
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public ICollection<Personagem> Personagens { get; set; } = new List<Personagem>();
    }
}
