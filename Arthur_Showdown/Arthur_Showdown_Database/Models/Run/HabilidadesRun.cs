using Arthur_Showdown_Database.Models.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Run
{
    public class HabilidadesRun
    {
        public int Id { get; set; }
        public int PersonagemRunId { get; set; }
        public int HabilidadeId { get; set; }
        public PersonagemRun? PersonagemRun { get; set; }
        public Habilidade? Habilidade { get; set; }
    }
}
