using Arthur_Showdown_Database.Models.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Auxiliar
{
    public class PersonagemAtaque
    {
        public int PersonagemId { get; set; }
        public Personagem? Personagem { get; set; }

        public int AtaqueId { get; set; }
        public Ataque? Ataque { get; set; }
    }
}
