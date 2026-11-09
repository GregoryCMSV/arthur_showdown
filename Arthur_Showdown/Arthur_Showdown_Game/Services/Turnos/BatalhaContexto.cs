using Arthur_Showdown_Game.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services;

public class BatalhaContexto
{
    public CharacterModel Jogador { get; init; } = default!;
    public int VidaAtual { get; set; }
    public List<InimigoEmCombate> Inimigos { get; set; } = new();
    public int Turno { get; set; }
    public int TilesTotal { get; init; } = 5;
}
