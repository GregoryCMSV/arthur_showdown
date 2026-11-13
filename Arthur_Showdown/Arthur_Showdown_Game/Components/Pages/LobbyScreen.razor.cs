using Arthur_Showdown_Game.Components.Game;
using Arthur_Showdown_Game.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Components.Pages;

public partial class LobbyScreen : ComponentBase, IDisposable
{
    private enum ModalAberto { Nenhum, Mapa, Loja, Scoreboard }

    [Inject] private ImageService _ImgService { get; set; } = default!;
    [Inject] private InputManager _Controle { get; set; } = default!;
    [Inject] public PlayerService _PlayerService { get; set; } = default!;
    [Inject] public LoadingService _LoadingService { get; set; } = default!;
    [Inject] public TurnManager _Turnos { get; set; } = default!;
    [Inject] public SoundService _Som { get; set; } = default!;
    [Inject] public NavigationService NavService { get; set; } = default!;

    private ModalAberto JanelaAberta = ModalAberto.Nenhum;

    private int TileTremendoId = -1;

    public ElementReference GameplayContainer { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await _PlayerService.LoadCharacters();
            if (_PlayerService.CurrentCharacter != null)
            {
                _PlayerService.CurrentCharacter.ModoCombate = false;
                _PlayerService.CurrentCharacter.LimiteMapa = 5;
                var iniciais = (_PlayerService.CurrentCharacter.GetAtaques() ?? new())
                    .Select(a => new CartaAtaque { Ataque = a })
                    .ToList();
                _Turnos.ResetarParaLobby(
                    iniciais,
                    _PlayerService.CurrentCharacter,
                    ehFlecha => _ = _Som.TocarHitAsync(ehFlecha),
                    () => _ = _Som.TocarMovimentoAsync());
                _PlayerService.CurrentCharacter.OnChange += StatusMudou;
            }
            _Controle.AoSolicitarMovimento += ProcessarMovimento;
            _Controle.AoSolicitarInteracao += AoExecutarFilaLobby;
            _Controle.AoSolicitarEsperar += AoEsperarLobby;
            _ = _Som.IniciarMusicaAsync();
        }
        finally
        {
            _LoadingService.FinalizarCarregamento();
        }
    }

    private void AbrirMapa()
    {
        JanelaAberta = ModalAberto.Mapa;
        StateHasChanged();
    }

    private void VoltarMenuInicial()
    {
        NavService.GoToHome();
    }

    private void StatusMudou()
    {
        var charModel = _PlayerService.CurrentCharacter;
        if (charModel == null) return;
        if (charModel.EstaEmMovimento)
        {
            JanelaAberta = ModalAberto.Nenhum;
            _ = _Som.TocarMovimentoAsync();
            ReduzirCooldownsLobby();
        }
        else
        {
            ProcessarEventosDoTile(charModel.PosicaoPersonagem);
        }

        InvokeAsync(StateHasChanged);
    }

    private void ReduzirCooldownsLobby()
    {
        bool alterou = false;
        foreach (var c in _Turnos.Cartas)
        {
            if (c.CooldownRestante > 0)
            {
                c.CooldownRestante--;
                alterou = true;
            }
        }
        if (alterou)
            _Turnos.NotificarMudanca();
    }

    private void AoExecutarFilaLobby()
    {
        if (JanelaAberta != ModalAberto.Nenhum) return;
        _ = _Turnos.JogadorAgirAsync(new ExecutarFilaAcao(_Turnos.Cartas));
    }

    private void AoEsperarLobby()
    {
        if (JanelaAberta != ModalAberto.Nenhum) return;
        _ = _Turnos.JogadorAgirAsync(new EsperarAcao());
    }

    private void ProcessarMovimento(int direcao)
    {
        if (JanelaAberta != ModalAberto.Mapa)
        {
            JanelaAberta = ModalAberto.Nenhum;
        }
        else if (JanelaAberta == ModalAberto.Mapa)
        {
            JanelaAberta = ModalAberto.Nenhum;
            StateHasChanged();
            return;
        }
    }

    private async void ProcessarEventosDoTile(int posicao)
    {
        switch (posicao)
        {
            case 0:
               // JanelaAberta = ModalAberto.Loja;
                break;
            case 1:
              //  JanelaAberta = ModalAberto.Scoreboard;
                break;
            case 2:
                TileTremendoId = posicao;
                StateHasChanged();
                await Task.Delay(200);
                TileTremendoId = -1;
                break;
            case 3:
            case 4:
                break;
        }
    }

    public void Dispose()
    {
        _Controle.AoSolicitarMovimento -= ProcessarMovimento;
        _Controle.AoSolicitarInteracao -= AoExecutarFilaLobby;
        _Controle.AoSolicitarEsperar -= AoEsperarLobby;
        if (_PlayerService.CurrentCharacter != null)
        {
            _PlayerService.CurrentCharacter.OnChange -= StatusMudou;
            if (!_PlayerService.CurrentCharacter.ModoCombate)
            {
                foreach (var c in _Turnos.Cartas)
                {
                    c.CooldownRestante = 0;
                    c.PosicaoFila = null;
                }
                if (_Turnos.Ctx != null)
                    _Turnos.Ctx.EscudoAtivo = false;
            }
        }
    }

    private async Task FecharModal()
    {
        JanelaAberta = ModalAberto.Nenhum;
        StateHasChanged();
        await Task.Delay(10);
    }
}
