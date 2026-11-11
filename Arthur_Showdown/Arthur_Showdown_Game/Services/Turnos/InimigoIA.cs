using System;
using System.Collections.Generic;
using System.Linq;

namespace Arthur_Showdown_Game.Services;

public enum DecisaoInimigo { Esperar, Virar, Preparar, Mover, Recuar, Telegrafar, Atacar }

public static class InimigoIA
{
    private const int AlcanceCorpoACorpo = 2;

    public static DecisaoInimigo Decidir(InimigoEmCombate e, int posJogador, IEnumerable<InimigoEmCombate> todos, int tilesTotal)
    {
        int pos = e.Estado.Posicao;
        int dist = Math.Abs(posJogador - pos);
        int rumo = Rumo(e, posJogador);

        if (e.RecuoPendente)
            return PodeRecuar(e, posJogador, todos, tilesTotal) ? DecisaoInimigo.Recuar : DecisaoInimigo.Esperar;
        if (e.RecuoSeProximo > 0 && dist == 1 && !e.Telegrafando && PodeRecuar(e, posJogador, todos, tilesTotal))
            return DecisaoInimigo.Recuar;

        if (e.Estado.Direcao != rumo) return DecisaoInimigo.Virar;
        if (!e.Preparado) return DecisaoInimigo.Preparar;

        if (e.Telegrafando) return DecisaoInimigo.Atacar;
        if (PodeAtacar(e, posJogador, todos, tilesTotal)) return DecisaoInimigo.Telegrafar;

        return PodeAndar(e, rumo, posJogador, todos, tilesTotal) ? DecisaoInimigo.Mover : DecisaoInimigo.Esperar;
    }

    public static int Rumo(InimigoEmCombate e, int posJogador)
    {
        int r = Math.Sign(posJogador - e.Estado.Posicao);
        return r == 0 ? 1 : r;
    }

    public static bool PodeAtacar(InimigoEmCombate e, int posJogador, IEnumerable<InimigoEmCombate> todos, int tilesTotal)
    {
        var hitbox = HitboxHelper.Ler(e.Ataque);
        var tiles = HitboxHelper.Tiles(hitbox, e.Estado.Posicao, e.Estado.Direcao, tilesTotal);
        if (!tiles.Contains(posJogador)) return false;

        if (hitbox.AlcanceMax > AlcanceCorpoACorpo)
        {
            int min = Math.Min(e.Estado.Posicao, posJogador);
            int max = Math.Max(e.Estado.Posicao, posJogador);
            if (todos.Any(o => o != e && o.Vivo && o.Estado.Posicao > min && o.Estado.Posicao < max))
                return false;
        }
        return true;
    }

    public static bool PodeAndar(InimigoEmCombate e, int direcao, int posJogador, IEnumerable<InimigoEmCombate> todos, int tilesTotal)
    {
        int novo = e.Estado.Posicao + direcao;
        return novo >= 0 && novo < tilesTotal
            && novo != posJogador
            && !todos.Any(o => o != e && o.Vivo && o.Estado.Posicao == novo);
    }

    private static bool PodeRecuar(InimigoEmCombate e, int posJogador, IEnumerable<InimigoEmCombate> todos, int tilesTotal)
        => PodeAndar(e, -Rumo(e, posJogador), posJogador, todos, tilesTotal);
}
