using Arthur_Showdown_Game.Components.Game;
using Arthur_Showdown_Game.Models;
using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
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
        private int _localId;
        private int _posInicial = 1;
        private int _sessaoNumero = 1;
        private int _totalSessoes = 1;
        private int _waveNumero = 1;
        private int _totalWaves = 1;
        private bool _pronto;
        private string? _erro;
        private bool _assinado;

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

                var estado = await Http.GetEstado(_gameId);
                if (estado == null) { _erro = "Não foi possível carregar o jogo."; return; }

                AplicarEstado(model, estado);

                Turnos.OnMudou += AoMudar;
                Turnos.OnTurnoFinalizado += AoTurnoFinalizado;
                Input.AoSolicitarMovimento += AoMover;
                Input.AoSolicitarTrocaDirecao += AoVirar;
                Input.AoSolicitarInteracao += AoExecutarFila;
                _assinado = true;

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

        private void AplicarEstado(CharacterModel model, EstadoJogoDto estado)
        {
            int tiles = estado.TamanhoMapa > 0 ? estado.TamanhoMapa : 5;
            _vidaMax = estado.VidaMaxima;
            _ouro = estado.OuroAtual;
            _localId = estado.LocalAtualId;
            _posInicial = estado.Posicao;
            _sessaoNumero = estado.SessaoNumero > 0 ? estado.SessaoNumero : 1;
            _totalSessoes = estado.TotalSessoes > 0 ? estado.TotalSessoes : 1;
            _waveNumero = estado.WaveNumero > 0 ? estado.WaveNumero : 1;
            _totalWaves = estado.TotalWaves > 0 ? estado.TotalWaves : 1;

            model.PosicaoPersonagem = estado.Posicao;
            model.DirecaoOlhar = estado.DirecaoOlhar == 0 ? 1 : estado.DirecaoOlhar;
            model.LimiteMapa = tiles;
            model.ModoCombate = true;

            var cartas = estado.Deck.Select(d => new CartaAtaque
            {
                DeckAtaquesId = d.DeckAtaquesId,
                CooldownRestante = d.CooldownRestante,
                PosicaoFila = d.PosicaoFila,
                Ataque = d.Ataque
            }).ToList();

            var inimigos = estado.Inimigos.Select(i => new InimigoEmCombate(i)).ToList();

            Turnos.Iniciar(new BatalhaContexto
            {
                Jogador = model,
                VidaAtual = estado.VidaAtual,
                Turno = estado.Turno,
                TilesTotal = tiles,
                Inimigos = inimigos
            }, cartas);
        }

        private void AoMover(int dir) => _ = Turnos.JogadorAgirAsync(new MoverAcao(dir));
        private void AoVirar() => _ = Turnos.JogadorAgirAsync(new VirarAcao());
        private void AoExecutarFila() => _ = Turnos.JogadorAgirAsync(new ExecutarFilaAcao(Turnos.Cartas));

        private void AoMudar() => InvokeAsync(StateHasChanged);

        private async void AoTurnoFinalizado()
        {
            try
            {
                bool limpouWave = Turnos.Ctx.Inimigos.Count == 0 && Turnos.Ctx.VidaAtual > 0;
                var novoEstado = await Http.SalvarEstado(_gameId, GerarSnapshot());

                if (Turnos.Ctx.VidaAtual <= 0)
                {
                    await Http.FinalizarGame(_gameId, vitoria: false);
                    return;
                }

                // Se limpou a wave e o servidor spawnou a proxima wave/sessao, atualiza o contexto
                if (limpouWave && novoEstado != null && !novoEstado.Vitoria && novoEstado.Inimigos.Count > 0 && _PlayerService.CurrentCharacter != null)
                {
                    AplicarEstado(_PlayerService.CurrentCharacter, novoEstado);
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch { }
        }

        private EstadoJogoDto GerarSnapshot()
        {
            var ctx = Turnos.Ctx;
            return new EstadoJogoDto
            {
                GameId = _gameId,
                OuroAtual = _ouro,
                LocalAtualId = _localId,
                Turno = ctx.Turno,
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
                    PosicaoFila = c.PosicaoFila
                }).ToList(),
                Inimigos = ctx.Inimigos.Where(i => i.Vivo).Select(i => i.Estado).ToList()
            };
        }

        private void VoltarLobby() => NavService.GoToLobby();

        public void Dispose()
        {
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
