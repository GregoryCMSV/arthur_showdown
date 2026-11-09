using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;

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

    public InimigoAcaoDto? Proxima => Fila.Count == 0 ? null : Fila[Estado.CursorAcao % Fila.Count];

    public void Avancar()
    {
        if (Fila.Count > 0) Estado.CursorAcao = (Estado.CursorAcao + 1) % Fila.Count;
    }

    public void ReceberDano(int d) => Estado.VidaAtual = Math.Max(0, Estado.VidaAtual - d);
}
