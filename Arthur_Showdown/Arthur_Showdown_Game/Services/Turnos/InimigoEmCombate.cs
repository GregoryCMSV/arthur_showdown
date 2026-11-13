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

    public bool Amaldicoado
    {
        get => Estado.Amaldicoado;
        set => Estado.Amaldicoado = value;
    }

    public bool EfeitoEspelhoAtivo
    {
        get => Estado.EfeitoEspelhoAtivo;
        set => Estado.EfeitoEspelhoAtivo = value;
    }

    public DecisaoInimigo? IntencaoPlanejada { get; set; }
    public int DirecaoIntencao { get; set; } = 1;
    public int OrdemTurno { get; set; } = 1;
    public int? UltimoDanoRecebido { get; set; }
    public int DanoRecebidoId { get; set; }

    private InimigoAcaoDto? AcaoAtacar => Fila.FirstOrDefault(a => a.Tipo == "Atacar");

    public List<AtaqueDto> AtaquesNaFila => Fila
        .Where(a => a.Tipo == "Atacar")
        .Select(a =>
        {
            if (a.Ataque != null)
            {
                return new AtaqueDto
                {
                    Id = a.Ataque.Id,
                    Nome = a.Ataque.Nome,
                    Descricao = a.Ataque.Descricao,
                    Dano = a.Ataque.Dano,
                    Cooldown = 0,
                    IsFastAction = false,
                    LimiteAprimoramento = a.Ataque.LimiteAprimoramento,
                    HitboxJson = a.Ataque.HitboxJson,
                    ImagemUrl = string.IsNullOrEmpty(a.Ataque.ImagemUrl) ? Estado.ImagemUrl : a.Ataque.ImagemUrl
                };
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
    public int InvocarAposAtaque => Fila.FirstOrDefault(a => a.Tipo == "InvocarAposAtaque")?.Valor
        ?? (Estado.Nome.Contains("Tank", StringComparison.OrdinalIgnoreCase) ? 2 : 0);

    public InimigoAcaoDto? Proxima => Fila.Count == 0 ? null : Fila[Estado.CursorAcao % Fila.Count];

    public void Avancar()
    {
        if (Fila.Count > 0) Estado.CursorAcao = (Estado.CursorAcao + 1) % Fila.Count;
    }

    public int ReceberDano(int d)
    {
        if (d <= 0) return 0;
        int danoFinal = Estado.Amaldicoado ? d * 2 : d;
        if (Estado.Amaldicoado)
            Estado.Amaldicoado = false;
        UltimoDanoRecebido = danoFinal;
        DanoRecebidoId++;
        Estado.VidaAtual = Math.Max(0, Estado.VidaAtual - danoFinal);
        return danoFinal;
    }
}
