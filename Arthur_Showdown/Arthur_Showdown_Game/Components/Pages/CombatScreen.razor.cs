using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
/* inicio flag */
using Arthur_Showdown_Game.Components.Game;
/*fim flag*/

namespace Arthur_Showdown_Game.Components.Pages
{
    public partial class CombatScreen : IDisposable
    {
        [Inject] public NavigationService NavService { get; set; }

        [Parameter] public int? GameId { get; set; }
        [Inject] public LoadingService _LoadingService { get; set; }
        [Inject] public HTTPService Http { get; set; } = default!;
        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Inject] public InputManager Input { get; set; } = default!;
        [Inject] public TurnManager Turnos { get; set; } = default!;

        private int _gameId;
        private int _vidaMax = 10;
        private int _ouro;
        private int _localId;
        private int _posInicial = 1;
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

                _vidaMax = estado.VidaMaxima;
                _ouro = estado.OuroAtual;
                _localId = estado.LocalAtualId;
                _posInicial = estado.Posicao;

                model.PosicaoPersonagem = estado.Posicao;
                model.DirecaoOlhar = estado.DirecaoOlhar == 0 ? 1 : estado.DirecaoOlhar;
                model.ModoCombate = true;   

                var cartas = estado.Deck.Select(d => new CartaAtaque
                {
                    DeckAtaquesId = d.DeckAtaquesId,
                    CooldownRestante = d.CooldownRestante,
                    Ataque = d.Ataque
                }).ToList();

                var inimigos = estado.Inimigos.Select(i => new InimigoEmCombate(i)).ToList();

                Turnos.Iniciar(new BatalhaContexto
                {
                    Jogador = model,
                    VidaAtual = estado.VidaAtual,
                    Turno = estado.Turno,
                    Inimigos = inimigos
                }, cartas);

                Turnos.OnMudou += AoMudar;
                Turnos.OnTurnoFinalizado += AoTurnoFinalizado;
                Input.AoSolicitarMovimento += AoMover;
                Input.AoSolicitarTrocaDirecao += AoVirar;
                Input.AoSolicitarInteracao += AoEsperar;
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

        private void AoMover(int dir) => _ = Turnos.JogadorAgirAsync(new MoverAcao(dir));
        private void AoVirar() => _ = Turnos.JogadorAgirAsync(new VirarAcao());
        private void AoEsperar() => _ = Turnos.JogadorAgirAsync(new EsperarAcao());

        private void AoMudar() => InvokeAsync(StateHasChanged);

        private async void AoTurnoFinalizado()
        {
            try
            {
                await Http.SalvarEstado(_gameId, GerarSnapshot());
                if (Turnos.Fase == FaseTurno.Fim && Turnos.Ctx.VidaAtual <= 0)
                    await Http.FinalizarGame(_gameId, vitoria: false);
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
                Deck = Turnos.Cartas.Select(c => new CartaEstadoDto
                {
                    DeckAtaquesId = c.DeckAtaquesId,
                    CooldownRestante = c.CooldownRestante
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
            Input.AoSolicitarInteracao -= AoEsperar;
            if (_PlayerService.CurrentCharacter != null) _PlayerService.CurrentCharacter.ModoCombate = false;
        }
    }

}
