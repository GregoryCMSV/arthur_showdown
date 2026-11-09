using Arthur_Showdown_Database.Models.Auxiliar;
using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Models.Base;
public class Ataque
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int Dano { get; set; }
    public int Cooldown { get; set; }
    public bool IsFastAction { get; set; }
    public int LimiteAprimoramento { get; set; }
    public int? EfeitoId { get; set; }
    public Efeito? Efeito { get; set; }
    public HitboxConfig Hitbox { get; set; } = new();
    public string? ImagemUrl { get; set; }
}


