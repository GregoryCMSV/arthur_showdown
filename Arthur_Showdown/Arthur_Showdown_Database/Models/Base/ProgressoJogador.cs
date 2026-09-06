using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Base
{
    public class ProgressoJogador
    {
        public Guid JogadorId { get; set; }
        public int EmblemasKamelot{ get; set; } = 0;
        public List<int> PersonagensDesbloqueadosIds { get; set; } = new();
        public List<int> AtaquesDesbloqueadosIds { get; set; } = new();
        public List<int> HabilidadesDesbloqueadasIds { get; set; } = new();
    }
}
