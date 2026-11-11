using Arthur_Showdown_Game.Components.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        ctx.NotificarVisual = () => OnMudou?.Invoke();
        Ctx = ctx;
        Cartas = cartas;
        Fase = ctx.Inimigos.Count == 0 || ctx.VidaAtual <= 0 ? FaseTurno.Fim : FaseTurno.Jogador;
        OnMudou?.Invoke();
    }

    public void NotificarMudanca() => OnMudou?.Invoke();

    public void ResetarParaLobby(List<CartaAtaque> cartasIniciais)
    {
        foreach (var c in cartasIniciais)
        {
            c.CooldownRestante = 0;
            c.PosicaoFila = null;
        }
        Cartas = cartasIniciais;
        Fase = FaseTurno.Jogador;
        OnMudou?.Invoke();
    }

    public void FinalizarPartida()
    {
        Fase = FaseTurno.Fim;
        OnMudou?.Invoke();
    }

    public void MoverNaFila(CartaAtaque carta, int delta)
    {
        if (Fase != FaseTurno.Jogador || !carta.EmFila) return;
        var fila = Cartas.Where(c => c.EmFila).OrderBy(c => c.PosicaoFila).ToList();
        int idx = fila.IndexOf(carta);
        int novo = idx + delta;
        if (idx < 0 || novo < 0 || novo >= fila.Count) return;

        (fila[idx].PosicaoFila, fila[novo].PosicaoFila) = (fila[novo].PosicaoFila, fila[idx].PosicaoFila);
        OnMudou?.Invoke();
        OnTurnoFinalizado?.Invoke();
    }

    public async Task JogadorAgirAsync(IAcaoDeTurno acao)
    {
        if (Fase != FaseTurno.Jogador) return;
        Fase = FaseTurno.Resolvendo; OnMudou?.Invoke();
        await acao.ExecutarAsync(Ctx);
        Ctx.Inimigos.RemoveAll(i => !i.Vivo);
        OnMudou?.Invoke();

        bool houveMudanca = acao.GastaTurno || acao is AdicionarNaFilaAcao || acao is RemoverDaFilaAcao || acao is ExecutarFilaAcao;
        if (acao.GastaTurno && Ctx.Inimigos.Count == 0)
        {
            await new AvancarFilaAcao(Cartas).ExecutarAsync(Ctx);
            OnMudou?.Invoke();
        }
        else if (acao.GastaTurno && Ctx.Inimigos.Count > 0)
        {
            var lista = new List<IAcaoDeTurno> { new AvancarFilaAcao(Cartas) };
            lista.AddRange(Ctx.Inimigos
                .Where(i => i.Vivo)
                .OrderBy(i => Math.Abs(i.Estado.Posicao - Ctx.Jogador.PosicaoPersonagem))
                .Select(i => new AcaoInimigoAcao(i)));
            foreach (var a in lista)
            {
                await a.ExecutarAsync(Ctx);
                Ctx.Inimigos.RemoveAll(i => !i.Vivo);
                OnMudou?.Invoke();
                if (Ctx.VidaAtual <= 0)
                {
                    Fase = FaseTurno.Fim;
                    OnMudou?.Invoke();
                    OnTurnoFinalizado?.Invoke();
                    return;
                }
            }
            Ctx.Turno++;
        }

        if (Ctx.VidaAtual <= 0)
            Fase = FaseTurno.Fim;
        else if (Ctx.Inimigos.Count > 0)
            Fase = FaseTurno.Jogador;
        OnMudou?.Invoke();
        if (houveMudanca) OnTurnoFinalizado?.Invoke();
    }
}
