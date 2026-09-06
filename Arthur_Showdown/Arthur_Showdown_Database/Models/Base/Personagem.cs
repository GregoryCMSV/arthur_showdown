using Arthur_Showdown_Database.Models.Auxiliar;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Base
{
    public class Personagem
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int TipoPersonagemId { get; set; }

        public TipoPersonagem? TipoPersonagem { get; set; }
    }
}
