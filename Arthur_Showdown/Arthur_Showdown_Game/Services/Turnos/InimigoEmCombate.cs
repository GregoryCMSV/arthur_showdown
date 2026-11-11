using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arthur_Showdown_Game.Services;

public class InimigoEmCombate
{
    public InimigoEstadoDto Estado { get; }
    public List<InimigoAcaoDto> Fila { get; }
    public bool Vivo => Estado.VidaAtual > 0;

    public InimigoEmCombate(InimigoEstadoDto estado)
    {
        Estado = estado;
        Fila = estado.Acoes.OrderBy(a => a.Ordem).ToList();
    }

    public bool Preparado
    {
        get => Estado.Preparado;
        set => Estado.Preparado = value;
    }

    public bool Telegrafando
    {
        get => Estado.Telegrafando;
        set => Estado.Telegrafando = value;
    }

    public bool RecuoPendente
    {
        get => Estado.RecuoPendente;
        set => Estado.RecuoPendente = value;
    }

    private InimigoAcaoDto? AcaoAtacar => Fila.FirstOrDefault(a => a.Tipo == "Atacar");

    public List<AtaqueDto> AtaquesNaFila => Fila
        .Where(a => a.Tipo == "Atacar")
        .Select(a =>
        {
            if (a.Ataque != null)
            {
                if (string.IsNullOrEmpty(a.Ataque.ImagemUrl))
                    a.Ataque.ImagemUrl = Estado.ImagemUrl;
                return a.Ataque;
            }
            return new AtaqueDto
            {
                Nome = "Ataque",
                ImagemUrl = Estado.ImagemUrl,
                Dano = a.Valor > 0 ? a.Valor : 1
            };
        })
        .DefaultIfEmpty(new AtaqueDto
        {
            Nome = "Ataque",
            ImagemUrl = Estado.ImagemUrl,
            Dano = Dano > 0 ? Dano : 2
        })
        .ToList();

    public AtaqueDto? Ataque => AcaoAtacar?.Ataque ?? AtaquesNaFila.FirstOrDefault();
    public int Dano => AcaoAtacar?.Ataque?.Dano ?? AcaoAtacar?.Valor ?? 2;
    public int RecuoAposAtaque => Fila.FirstOrDefault(a => a.Tipo == "RecuarAposAtaque")?.Valor ?? 0;
    public int RecuoSeProximo => Fila.FirstOrDefault(a => a.Tipo == "RecuarSeProximo")?.Valor ?? 0;

    public InimigoAcaoDto? Proxima => Fila.Count == 0 ? null : Fila[Estado.CursorAcao % Fila.Count];

    public void Avancar()
    {
        if (Fila.Count > 0) Estado.CursorAcao = (Estado.CursorAcao + 1) % Fila.Count;
    }

    public void ReceberDano(int d) => Estado.VidaAtual = Math.Max(0, Estado.VidaAtual - d);
}
