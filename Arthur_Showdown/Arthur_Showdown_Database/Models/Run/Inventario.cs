using Arthur_Showdown_Database.Models.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Run
{
    public class Inventario
    {
        public int Id { get; set; }
        public int PersonagemRunId { get; set; }
        public PersonagemRun? PersonagemRun { get; set; }
        public int ItemId { get; set; }
        public Item? Item { get; set; }
        public int Qtd { get; set; }
    }
}
