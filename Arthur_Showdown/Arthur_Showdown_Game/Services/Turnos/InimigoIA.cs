using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arthur_Showdown_Game.Services;

public enum DecisaoInimigo { Esperar, Virar, Preparar, Mover, Recuar, Telegrafar, Atacar }

public static class InimigoIA
{
    private const int AlcanceCorpoACorpo = 2;

    public static void PlanejarTurno(List<InimigoEmCombate> inimigos, int posJogador, int tilesTotal)
    {
        var vivosOrdenados = inimigos
            .Where(i => i.Vivo)
            .OrderBy(i => Math.Abs(i.Estado.Posicao - posJogador))
            .ThenBy(i => i.Estado.Posicao)
            .ToList();

        for (int idx = 0; idx < vivosOrdenados.Count; idx++)
        {
            vivosOrdenados[idx].OrdemTurno = idx + 1;
            vivosOrdenados[idx].IntencaoPlanejada = null;
        }

        foreach (var ini in vivosOrdenados)
        {
            int rumo = Rumo(ini, posJogador);
            ini.DirecaoIntencao = rumo;
            ini.IntencaoPlanejada = CalcularDecisao(ini, posJogador, vivosOrdenados, tilesTotal);
        }
    }

    public static DecisaoInimigo Decidir(InimigoEmCombate e, int posJogador, IEnumerable<InimigoEmCombate> todos, int tilesTotal)
    {
        if (e.IntencaoPlanejada.HasValue)
            return e.IntencaoPlanejada.Value;

        return CalcularDecisao(e, posJogador, todos, tilesTotal);
    }

    public static DecisaoInimigo CalcularDecisao(InimigoEmCombate e, int posJogador, IEnumerable<InimigoEmCombate> todos, int tilesTotal)
    {
        int pos = e.Estado.Posicao;
        int dist = Math.Abs(posJogador - pos);
        int rumo = Rumo(e, posJogador);

        // Se ja estava telegrafando, o ataque vai acontecer neste turno mesmo que o jogador se mova ou vire o inimigo com Espelho
        if (e.Telegrafando)
            return DecisaoInimigo.Atacar;

        if (e.RecuoPendente)
            return PodeRecuar(e, posJogador, todos, tilesTotal) ? DecisaoInimigo.Recuar : DecisaoInimigo.Esperar;
        if (e.RecuoSeProximo > 0 && dist == 1 && !e.Telegrafando && PodeRecuar(e, posJogador, todos, tilesTotal))
            return DecisaoInimigo.Recuar;

        if (e.Estado.Direcao != rumo) return DecisaoInimigo.Virar;
        if (!e.Preparado) return DecisaoInimigo.Preparar;

        if (PodeAtacar(e, posJogador, todos, tilesTotal)) return DecisaoInimigo.Telegrafar;

        // O Arqueiro nao deve se aproximar do jogador quando estiver com o ataque carregado
        if (EhArqueiro(e) && e.Preparado)
            return DecisaoInimigo.Esperar;

        // Somente o inimigo mais proximo do jogador (naquele lado) se aproxima por vez
        bool existeMaisProximoNoMesmoLado = todos.Any(o =>
            o != e &&
            o.Vivo &&
            Rumo(o, posJogador) == rumo &&
            Math.Abs(posJogador - o.Estado.Posicao) < dist &&
            (o.IntencaoPlanejada == DecisaoInimigo.Mover || o.Estado.Posicao == pos + rumo));

        if (existeMaisProximoNoMesmoLado)
            return DecisaoInimigo.Esperar;

        return PodeAndar(e, rumo, posJogador, todos, tilesTotal) ? DecisaoInimigo.Mover : DecisaoInimigo.Esperar;
    }

    public static bool EhArqueiro(InimigoEmCombate e)
    {
        var hitbox = HitboxHelper.Ler(e.Ataque);
        return hitbox.AlcanceMax > AlcanceCorpoACorpo
            || e.Estado.Nome.Contains("Arqueiro", StringComparison.OrdinalIgnoreCase)
            || (e.Ataque?.Nome.Contains("Flecha", StringComparison.OrdinalIgnoreCase) ?? false);
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
