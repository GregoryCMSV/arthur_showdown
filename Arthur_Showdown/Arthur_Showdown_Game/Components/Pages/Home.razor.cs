using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Arthur_Showdown_Shared.Dtos.Usuario;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Arthur_Showdown_Game.Components.Pages;

public partial class Home : ComponentBase
{
    private enum GameState { PressAnyKey, Autenticacao, MainMenu }
    private GameState EstadoAtual = GameState.PressAnyKey;
    private bool MostrarAvisoNovoJogo = false;
    private string? jogoAtivoId = "";

    [Inject] private ImageService ImgService { get; set; }
    [Inject] private HTTPService HttpService { get; set; }
    [Inject] private NavigationService NavService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await VerificarProgresso();
    }

    private async Task TentarPassarDaTelaInicial()
    {
        if (EstadoAtual == GameState.PressAnyKey)
        {
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                EstadoAtual = GameState.MainMenu;
            }
            else
            {
                EstadoAtual = GameState.Autenticacao;
            }
        }
    }

    private async Task IrParaMenuPrincipal()
    {
        await VerificarProgresso();
        EstadoAtual = GameState.MainMenu;
    }

    private async Task VerificarProgresso()
    {
        jogoAtivoId = await HttpService.GetCurrentGame();
    }

    private void ClicouNovoJogo()
    {
        if (TemJogoAtivo)
            MostrarAvisoNovoJogo = true;
        else
            NovoJogo();
    }

    private async Task ConfirmarSobrescreverJogo()
    {
        MostrarAvisoNovoJogo = false;
        await HttpService.AbadonGame();
        NovoJogo();
    }

    private void ContinuarJogo()
    {
        NavService.GoToGame(jogoAtivoId);
    }
    private void NovoJogo()
    {
        NavService.GoToLobby();
    }
    private void AbrirOpcoes() { }

    private void Sair()
    {
        SecureStorage.Default.Remove("auth_token");
        EstadoAtual = GameState.Autenticacao;
        jogoAtivoId = null;
        MostrarAvisoNovoJogo = false;
    }

    private bool TemJogoAtivo => !string.IsNullOrEmpty(jogoAtivoId);

}
