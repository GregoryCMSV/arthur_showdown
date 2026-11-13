using Arthur_Showdown_Game.Models;
using Arthur_Showdown_Shared.Dtos.Game;
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

public class DanoFlutuante
{
    public int Id { get; init; }
    public int PosicaoTile { get; init; }
    public int Valor { get; init; }
}

public class RaioInvocacao
{
    public int Id { get; init; }
    public int PosicaoTile { get; init; }
}

public class BatalhaContexto
{
    private int _proxDanoId;
    private int _proxRaioId;
    public CharacterModel Jogador { get; init; } = default!;
    public int VidaAtual { get; set; }
    public List<InimigoEmCombate> Inimigos { get; set; } = new();
    public List<InimigoEstadoDto> PoolInimigosInvocacao { get; init; } = new();
    public int Turno { get; set; }
    public int TilesTotal { get; init; } = 5;
    public int QtdMudancaDirecao { get; set; }
    public int QtdHitsSofridos { get; set; }
    public bool EscudoAtivo { get; set; }
    public bool TremendoTela { get; set; }
    public int? UltimoDanoJogador { get; set; }
    public int DanoJogadorId { get; set; }
    public bool IgnorarFimSemInimigos { get; set; }
    public bool ImpedirMorteJogador { get; set; }
    public List<DanoFlutuante> DanosFlutuantes { get; } = new();
    public List<RaioInvocacao> RaiosInvocacao { get; } = new();
    public EfeitoVisualAtaque? EfeitoVisual { get; set; }
    public Action<List<InimigoEmCombate>, int, int>? PlanejadorInimigosCustom { get; set; }
    public Action? NotificarVisual { get; set; }
    public Action<bool>? TocarSomHit { get; set; }
    public Action? TocarSomMovimento { get; set; }

    public void RegistrarRaiosInvocacao(IEnumerable<int> posicoes)
    {
        var novos = new List<RaioInvocacao>();
        foreach (var pos in posicoes.Distinct())
        {
            novos.Add(new RaioInvocacao
            {
                Id = ++_proxRaioId,
                PosicaoTile = pos
            });
        }
        if (novos.Count == 0) return;

        lock (RaiosInvocacao)
        {
            RaiosInvocacao.AddRange(novos);
        }
        NotificarVisual?.Invoke();
        _ = RemoverRaiosAposTempoAsync(novos);
    }

    private async System.Threading.Tasks.Task RemoverRaiosAposTempoAsync(List<RaioInvocacao> itens)
    {
        await System.Threading.Tasks.Task.Delay(520);
        lock (RaiosInvocacao)
        {
            foreach (var r in itens)
                RaiosInvocacao.Remove(r);
        }
        NotificarVisual?.Invoke();
    }

    public void RegistrarDanoFlutuante(int posTile, int dano)
    {
        if (dano <= 0) return;
        var item = new DanoFlutuante
        {
            Id = ++_proxDanoId,
            PosicaoTile = posTile,
            Valor = dano
        };
        lock (DanosFlutuantes)
        {
            DanosFlutuantes.Add(item);
        }
        _ = RemoverDanoAposTempoAsync(item);
    }

    private async System.Threading.Tasks.Task RemoverDanoAposTempoAsync(DanoFlutuante item)
    {
        await System.Threading.Tasks.Task.Delay(780);
        lock (DanosFlutuantes)
        {
            DanosFlutuantes.Remove(item);
        }
        NotificarVisual?.Invoke();
    }
}
