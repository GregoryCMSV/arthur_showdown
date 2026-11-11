using Arthur_Showdown_Game.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arthur_Showdown_Game.Services;

public class EfeitoVisualAtaque
{
    public HashSet<int> TilesImpacto { get; init; } = new();
    public bool EhFlecha { get; init; }
    public int OrigemTile { get; init; }
    public int DestinoTile { get; init; }
    public int Direcao { get; init; } = 1;
}

public class BatalhaContexto
{
    public CharacterModel Jogador { get; init; } = default!;
    public int VidaAtual { get; set; }
    public List<InimigoEmCombate> Inimigos { get; set; } = new();
    public int Turno { get; set; }
    public int TilesTotal { get; init; } = 5;
    public int QtdMudancaDirecao { get; set; }
    public int QtdHitsSofridos { get; set; }
    public EfeitoVisualAtaque? EfeitoVisual { get; set; }
    public Action? NotificarVisual { get; set; }
}
