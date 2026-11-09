using Arthur_Showdown_Game.Services;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Arthur_Showdown_Shared.Dtos.Game;

namespace Arthur_Showdown_Game.Components.Pages;

public partial class LobbyScreen : ComponentBase, IDisposable
{
    private enum ModalAberto { Nenhum, Mapa, Loja, Scoreboard }

    [Inject] private ImageService _ImgService { get; set; }
    [Inject] private InputManager _Controle { get; set; }
    [Inject] public PlayerService _PlayerService { get; set; }
    [Inject] public LoadingService _LoadingService { get; set; }

    private ModalAberto JanelaAberta = ModalAberto.Nenhum;

    // --- Estado da Arena ---
    private int TileTremendoId = -1;

    // --- Dados do Jogador ---
    
    public ElementReference GameplayContainer { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await _PlayerService.LoadCharacters();
        _Controle.AoSolicitarMovimento += ProcessarMovimento;
        _PlayerService.CurrentCharacter.OnChange += StatusMudou;
        _LoadingService.FinalizarCarregamento();
    }

    private void AbrirMapa()
    {
        JanelaAberta = ModalAberto.Mapa;
        StateHasChanged();
    }

    private void StatusMudou()
    {
        var charModel = _PlayerService.CurrentCharacter;
        if (charModel == null) return;
        if (charModel.EstaEmMovimento)
        {
            JanelaAberta = ModalAberto.Nenhum;
        }
        else
        {
            ProcessarEventosDoTile(charModel.PosicaoPersonagem);
        }

        InvokeAsync(StateHasChanged);
    }

    private async void ProcessarMovimento(int direcao)
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
                JanelaAberta = ModalAberto.Loja;
                break;
            case 1:
                JanelaAberta = ModalAberto.Scoreboard;
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
        //_Controle.AoSolicitarTrocaDirecao -= ProcessarTrocaDirecao;
    }

    private async Task FecharModal()
    {
        JanelaAberta = ModalAberto.Nenhum;
        StateHasChanged();
        await Task.Delay(10);
    }
}