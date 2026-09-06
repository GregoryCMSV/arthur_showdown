using Arthur_Showdown_Database.Models.Auxiliar;
using Arthur_Showdown_Database.Models.Base;

namespace Arthur_Showdown_Database.Models.Run
{
    public class DeckAtaques
    {
        public int Id { get; set; }
        public int PersonagemRunId { get; set; }
        public PersonagemRun? PersonagemRun { get; set; }
        public int AtaqueId { get; set; }
        public Ataque? Ataque { get; set; }
        public int NumeroDeUpgrades { get; set; }
        public int LimiteUpgrade { get; set; }
        public int ModDano { get; set; }
        public int ModCooldown { get; set; }
        public int? EfeitoId { get; set; }
        public Efeito? Efeito { get; set; }
        public int QtdUsosNaRun { get; set; }
        public int QtdVezesRemovidoFila { get; set; }
        public int QtdVezesAdicionadoFila { get; set; }
        public bool IsFastAction { get; set; }
    }
}
