using Arthur_Showdown_Game.Components.Game;
using Arthur_Showdown_Game.Models;
using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Components.Pages
{
    public partial class CombatScreen : IDisposable
    {
        [Inject] public NavigationService NavService { get; set; } = default!;

        [Parameter] public int? GameId { get; set; }
        [Inject] public LoadingService _LoadingService { get; set; } = default!;
        [Inject] public HTTPService Http { get; set; } = default!;
        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Inject] public InputManager Input { get; set; } = default!;
        [Inject] public TurnManager Turnos { get; set; } = default!;

        private int _gameId;
        private int _vidaMax = 10;
        private int _ouro;
        private int _totalOuroGanho;
        private int _localId;
        private int _posInicial = 1;
        private int _sessaoNumero = 1;
        private int _totalSessoes = 1;
        private int _waveNumero = 1;
        private int _totalWaves = 1;
        private int _tempoSegundos;
        private bool _pronto;
        private string? _erro;
        private bool _assinado;
        private PeriodicTimer? _timer;
        private CancellationTokenSource? _ctsTimer;

        private bool _escolhendoMelhoria;
        private TipoMelhoriaAtaque _melhoriaSorteada = TipoMelhoriaAtaque.MaisUmDano;
        private bool _escolhendoNovoAtaque;
        private List<AtaqueDto> _opcoesNovoAtaque = new();
        private EstadoJogoDto? _estadoPendentePosRecompensa;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (_PlayerService.CurrentCharacter == null)
                    await _PlayerService.LoadCharacters();

                var model = _PlayerService.CurrentCharacter;
                if (model?.Personagem == null) { _erro = "Nenhum personagem disponível."; return; }
                int? id = GameId;
                if (id == null)
                {
                    id = await Http.CriarGame(model.Personagem.Id);
                    if (id == null && int.TryParse(await Http.GetCurrentGame(), out var ativo)) id = ativo;
                }
                if (id == null) { _erro = "Não foi possível criar o jogo."; return; }
                _gameId = id.Value;

                // Ao iniciar combate ou voltar do login, o cooldown dos ataques vem sempre liberado
                var estado = await Http.GetEstado(_gameId, resetCooldown: true);
                if (estado == null) { _erro = "Não foi possível carregar o jogo."; return; }

                AplicarEstado(model, estado);

                Turnos.OnMudou += AoMudar;
                Turnos.OnTurnoFinalizado += AoTurnoFinalizado;
                Input.AoSolicitarMovimento += AoMover;
                Input.AoSolicitarTrocaDirecao += AoVirar;
                Input.AoSolicitarInteracao += AoExecutarFila;
                _assinado = true;

                IniciarRelogio();
                _pronto = true;
            }
            catch (Exception ex)
            {
                _erro = $"Erro: {ex.Message}";
            }
            finally
            {
                _LoadingService.FinalizarCarregamento();
            }
        }

        private void IniciarRelogio()
        {
            _ctsTimer = new CancellationTokenSource();
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            _ = Task.Run(async () =>
            {
                try
                {
                    while (await _timer.WaitForNextTickAsync(_ctsTimer.Token))
                    {
                        if (Turnos.Fase != FaseTurno.Fim)
                        {
                            _tempoSegundos++;
                            await InvokeAsync(StateHasChanged);
                        }
                    }
                }
                catch { }
            });
        }

        private static string FormatarTempo(int seg)
            => $"{seg / 60:00}:{seg % 60:00}";

        private static string DescricaoMelhoria(TipoMelhoriaAtaque t) => t switch
        {
            TipoMelhoriaAtaque.MaisUmDano => "Melhoria sorteada: +1 de Dano no golpe selecionado",
            TipoMelhoriaAtaque.MaisDoisDanoMaisUmCooldown => "Melhoria sorteada: +2 de Dano e +1 de Cooldown no golpe selecionado",
            TipoMelhoriaAtaque.MenosUmCooldown => "Melhoria sorteada: -1 de Cooldown no golpe selecionado",
            TipoMelhoriaAtaque.AcaoRapida => "Melhoria sorteada: Transformar o golpe selecionado em Ação Rápida",
            _ => ""
        };

        private void AplicarEstado(CharacterModel model, EstadoJogoDto estado)
        {
            int tiles = estado.TamanhoMapa > 0 ? estado.TamanhoMapa : 5;
            _vidaMax = estado.VidaMaxima;
            _ouro = estado.OuroAtual;
            _totalOuroGanho = Math.Max(_totalOuroGanho, estado.TotalOuroGanho);
            _localId = estado.LocalAtualId;
            _posInicial = estado.Posicao;
            _sessaoNumero = estado.SessaoNumero > 0 ? estado.SessaoNumero : 1;
            _totalSessoes = estado.TotalSessoes > 0 ? estado.TotalSessoes : 1;
            _waveNumero = estado.WaveNumero > 0 ? estado.WaveNumero : 1;
            _totalWaves = estado.TotalWaves > 0 ? estado.TotalWaves : 1;
            _tempoSegundos = Math.Max(_tempoSegundos, estado.TempoSegundos);

            model.PosicaoPersonagem = estado.Posicao;
            model.DirecaoOlhar = estado.DirecaoOlhar == 0 ? 1 : estado.DirecaoOlhar;
            model.LimiteMapa = tiles;
            model.ModoCombate = true;

            var cartas = estado.Deck
                .Where(d => d.Ataque != null)
                .Select(d => new CartaAtaque
                {
                    DeckAtaquesId = d.DeckAtaquesId,
                    CooldownRestante = d.CooldownRestante,
                    PosicaoFila = d.PosicaoFila,
                    QtdUsosNaRun = d.QtdUsosNaRun,
                    QtdVezesAdicionadoFila = d.QtdVezesAdicionadoFila,
                    QtdVezesRemovidoFila = d.QtdVezesRemovidoFila,
                    Ataque = d.Ataque!
                }).ToList();

            var inimigos = estado.Inimigos.Select(i => new InimigoEmCombate(i)).ToList();

            Turnos.Iniciar(new BatalhaContexto
            {
                Jogador = model,
                VidaAtual = estado.VidaAtual,
                Turno = estado.Turno,
                TilesTotal = tiles,
                QtdMudancaDirecao = estado.QtdMudancaDirecao,
                QtdHitsSofridos = estado.QtdHitsSofridos,
                Inimigos = inimigos
            }, cartas);
        }

        private bool BloqueadoPorModal => _escolhendoMelhoria || _escolhendoNovoAtaque;

        private void AoMover(int dir)
        {
            if (BloqueadoPorModal) return;
            _ = Turnos.JogadorAgirAsync(new MoverAcao(dir));
        }

        private void AoVirar()
        {
            if (BloqueadoPorModal) return;
            _ = Turnos.JogadorAgirAsync(new VirarAcao());
        }

        private void AoExecutarFila()
        {
            if (BloqueadoPorModal) return;
            _ = Turnos.JogadorAgirAsync(new ExecutarFilaAcao(Turnos.Cartas));
        }

        private void AoMudar() => InvokeAsync(StateHasChanged);

        private async void AoTurnoFinalizado()
        {
            try
            {
                bool limpouWave = Turnos.Ctx.Inimigos.Count == 0 && Turnos.Ctx.VidaAtual > 0;
                int sessaoAntes = _sessaoNumero;

                var novoEstado = await Http.SalvarEstado(_gameId, GerarSnapshot());

                if (Turnos.Ctx.VidaAtual <= 0)
                {
                    await Http.FinalizarGame(_gameId, vitoria: false);
                    return;
                }

                if (!limpouWave) return;

                if (novoEstado == null)
                {
                    // Fallback caso o PUT nao tenha retornado o novo estado: recarrega do servidor
                    novoEstado = await Http.GetEstado(_gameId);
                }

                if (novoEstado == null || _PlayerService.CurrentCharacter == null) return;

                if (novoEstado.Vitoria)
                {
                    AplicarEstado(_PlayerService.CurrentCharacter, novoEstado);
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                _estadoPendentePosRecompensa = novoEstado;
                bool mudouSessao = novoEstado.SessaoNumero > sessaoAntes;

                if (mudouSessao)
                {
                    var opcoes = await Http.ObterOpcoesNovoAtaque(_gameId);
                    if (opcoes != null && opcoes.Count > 0)
                    {
                        _opcoesNovoAtaque = opcoes;
                        _escolhendoNovoAtaque = true;
                        await InvokeAsync(StateHasChanged);
                        return;
                    }
                }
                else
                {
                    var valores = Enum.GetValues<TipoMelhoriaAtaque>();
                    _melhoriaSorteada = valores[Random.Shared.Next(valores.Length)];
                    _escolhendoMelhoria = true;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                AplicarEstado(_PlayerService.CurrentCharacter, novoEstado);
                await InvokeAsync(StateHasChanged);
            }
            catch { }
        }

        private async Task SelecionarMelhoria(CartaAtaque c)
        {
            _escolhendoMelhoria = false;
            var atualizado = await Http.AplicarMelhoria(_gameId, c.DeckAtaquesId, _melhoriaSorteada)
                             ?? _estadoPendentePosRecompensa;
            _estadoPendentePosRecompensa = null;

            if (atualizado != null && _PlayerService.CurrentCharacter != null)
                AplicarEstado(_PlayerService.CurrentCharacter, atualizado);

            await InvokeAsync(StateHasChanged);
        }

        private async Task SelecionarNovoAtaque(AtaqueDto atk)
        {
            _escolhendoNovoAtaque = false;
            var atualizado = await Http.EscolherNovoAtaque(_gameId, atk.Id)
                             ?? _estadoPendentePosRecompensa;
            _estadoPendentePosRecompensa = null;

            if (atualizado != null && _PlayerService.CurrentCharacter != null)
                AplicarEstado(_PlayerService.CurrentCharacter, atualizado);

            await InvokeAsync(StateHasChanged);
        }

        private EstadoJogoDto GerarSnapshot()
        {
            var ctx = Turnos.Ctx;
            return new EstadoJogoDto
            {
                GameId = _gameId,
                OuroAtual = _ouro,
                TotalOuroGanho = _totalOuroGanho,
                LocalAtualId = _localId,
                Turno = ctx.Turno,
                TempoSegundos = _tempoSegundos,
                QtdMudancaDirecao = ctx.QtdMudancaDirecao,
                QtdHitsSofridos = ctx.QtdHitsSofridos,
                VidaAtual = ctx.VidaAtual,
                VidaMaxima = _vidaMax,
                Posicao = ctx.Jogador.PosicaoPersonagem,
                DirecaoOlhar = ctx.Jogador.DirecaoOlhar,
                TamanhoMapa = ctx.TilesTotal,
                SessaoNumero = _sessaoNumero,
                TotalSessoes = _totalSessoes,
                WaveNumero = _waveNumero,
                TotalWaves = _totalWaves,
                Deck = Turnos.Cartas.Select(c => new CartaEstadoDto
                {
                    DeckAtaquesId = c.DeckAtaquesId,
                    CooldownRestante = c.CooldownRestante,
                    PosicaoFila = c.PosicaoFila,
                    QtdUsosNaRun = c.QtdUsosNaRun,
                    QtdVezesAdicionadoFila = c.QtdVezesAdicionadoFila,
                    QtdVezesRemovidoFila = c.QtdVezesRemovidoFila
                }).ToList(),
                Inimigos = ctx.Inimigos.Where(i => i.Vivo).Select(i => i.Estado).ToList()
            };
        }

        private void VoltarLobby() => NavService.GoToLobby();

        public void Dispose()
        {
            try { _ctsTimer?.Cancel(); _timer?.Dispose(); _ctsTimer?.Dispose(); } catch { }
            if (!_assinado) return;
            Turnos.OnMudou -= AoMudar;
            Turnos.OnTurnoFinalizado -= AoTurnoFinalizado;
            Input.AoSolicitarMovimento -= AoMover;
            Input.AoSolicitarTrocaDirecao -= AoVirar;
            Input.AoSolicitarInteracao -= AoExecutarFila;
            if (_PlayerService.CurrentCharacter != null)
            {
                _PlayerService.CurrentCharacter.ModoCombate = false;
                _PlayerService.CurrentCharacter.LimiteMapa = 5;
            }
        }
    }
}
