namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class InimigoAcaoDto
    {
        public int Ordem { get; set; }
        public string Tipo { get; set; } = "Esperar";   // Esperar | Mover | Preparar | Atacar
        public AtaqueDto? Ataque { get; set; }
        public int Valor { get; set; }                  // Mover: tiles na direcao do jogador | Atacar sem Ataque: dano
    }
}
