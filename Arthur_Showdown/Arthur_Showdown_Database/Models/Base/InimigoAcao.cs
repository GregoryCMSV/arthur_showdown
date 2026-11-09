using Arthur_Showdown_Database.Models.Run;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Base
{
    public class InimigoAcao
    {
        public int Id { get; set; }
        public int PersonagemId { get; set; }
        public Personagem? Personagem { get; set; }
        public int Ordem { get; set; }
        public TipoAcaoInimigo Tipo { get; set; }
        public int? AtaqueId { get; set; }
        public Ataque? Ataque { get; set; }
        public int Valor { get; set; }
    }
}
