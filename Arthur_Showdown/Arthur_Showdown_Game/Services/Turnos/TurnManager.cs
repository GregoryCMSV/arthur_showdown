using Arthur_Showdown_Game.Components.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services;

public class TurnManager
{
    public event Action? OnMudou;
    public event Action? OnTurnoFinalizado;
    public FaseTurno Fase { get; private set; } = FaseTurno.Jogador;
    public List<CartaAtaque> Cartas { get; set; } = new();
    public BatalhaContexto Ctx { get; set; } = default!;

    public void Iniciar(BatalhaContexto ctx, List<CartaAtaque> cartas)
    {
        Ctx = ctx;
        Cartas = cartas;
        Fase = ctx.Inimigos.Count == 0 || ctx.VidaAtual <= 0 ? FaseTurno.Fim : FaseTurno.Jogador;
    }

    public async Task JogadorAgirAsync(IAcaoDeTurno acao)
    {
        if (Fase != FaseTurno.Jogador) return;
        Fase = FaseTurno.Resolvendo; OnMudou?.Invoke();
        await acao.ExecutarAsync(Ctx);
        Ctx.Inimigos.RemoveAll(i => !i.Vivo);
        OnMudou?.Invoke();  
        bool houveMudanca = acao.GastaTurno || acao is AtaqueAcao;
        if (acao.GastaTurno)
        {

            var lista = new List<IAcaoDeTurno> { new AvancarFilaAcao(Cartas) };
            lista.AddRange(Ctx.Inimigos
                .OrderBy(i => Math.Abs(i.Estado.Posicao - Ctx.Jogador.PosicaoPersonagem))
                .Select(i => new AcaoInimigoAcao(i)));
            foreach (var a in lista)
            {
                await a.ExecutarAsync(Ctx);
                OnMudou?.Invoke();
                if (Ctx.VidaAtual <= 0) { Fase = FaseTurno.Fim; OnMudou?.Invoke(); OnTurnoFinalizado?.Invoke(); return; }
            }
            Ctx.Turno++;
        }
        Fase = Ctx.Inimigos.Count == 0 ? FaseTurno.Fim : FaseTurno.Jogador;
        OnMudou?.Invoke();
        if (houveMudanca) OnTurnoFinalizado?.Invoke();
    }
}
