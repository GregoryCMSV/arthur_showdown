using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Arthur_Showdown_Game.Services;

public static class HitboxHelper
{
    public static HitboxConfig Ler(AtaqueDto? a)
    {
        if (a == null || string.IsNullOrWhiteSpace(a.HitboxJson)) return new HitboxConfig();
        try { return JsonSerializer.Deserialize<HitboxConfig>(a.HitboxJson) ?? new HitboxConfig(); }
        catch { return new HitboxConfig(); }
    }

    public static HashSet<int> Tiles(AtaqueDto a, int pos, int olhar, int total)
        => Tiles(Ler(a), pos, olhar, total);

    public static HashSet<int> Tiles(HitboxConfig h, int pos, int olhar, int total)
    {
        var set = new HashSet<int>();
        var dirs = h.Direcao switch
        {
            HitboxDirecao.Forward => new[] { olhar },
            HitboxDirecao.Backward => new[] { -olhar },
            _ => new[] { olhar, -olhar }
        };
        int origem = h.Origem switch
        {
            HitboxOrigem.EdgeFront => olhar > 0 ? total - 1 : 0,
            HitboxOrigem.EdgeBack => olhar > 0 ? 0 : total - 1,
            _ => pos
        };
        foreach (var d in dirs)
            for (int r = h.AlcanceMin; r <= h.AlcanceMax; r++)
            {
                int t = origem + d * r;
                if (t >= 0 && t < total) set.Add(t);
            }
        return set;
    }
}
