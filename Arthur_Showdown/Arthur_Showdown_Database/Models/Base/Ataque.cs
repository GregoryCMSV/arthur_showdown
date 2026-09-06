using Arthur_Showdown_Database.Models.Auxiliar;
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
}
public class HitboxConfig
{
    public HitboxOrigem Origem { get; set; } = HitboxOrigem.Caster;
    public HitboxDirecao Direcao { get; set; } = HitboxDirecao.Forward;
    public int AlcanceMin { get; set; } = 1;
    public int AlcanceMax { get; set; } = 1;
}

public enum HitboxOrigem
{
    Caster,     
    EdgeFront,  
    EdgeBack    
}

public enum HitboxDirecao
{
    Forward,    
    Backward,   
    Both        
}

