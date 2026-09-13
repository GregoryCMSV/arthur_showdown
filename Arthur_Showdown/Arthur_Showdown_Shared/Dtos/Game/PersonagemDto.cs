using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class PersonagemDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public List<AtaqueDto> AtaquesIniciais { get; set; } = new();
    }
}
