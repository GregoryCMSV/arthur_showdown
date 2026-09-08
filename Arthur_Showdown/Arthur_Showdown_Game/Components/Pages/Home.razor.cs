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
    private bool TemJogoAtivo = false;
    private bool MostrarAvisoNovoJogo = false;

    [Inject]
    private ImageService ImageService { get; set; }


    protected override async Task OnInitializedAsync()
    {
        var token = await SecureStorage.Default.GetAsync("auth_token");
        if (!string.IsNullOrEmpty(token))
        {
            Http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            await VerificarProgresso();
            EstadoAtual = GameState.MainMenu;
        }
    }

    private void TentarPassarDaTelaInicial()
    {
        if (EstadoAtual == GameState.PressAnyKey)
            EstadoAtual = GameState.Autenticacao;
    }

    private async Task IrParaMenuPrincipal()
    {
        var token = await SecureStorage.Default.GetAsync("auth_token");
        if (!string.IsNullOrEmpty(token))
        {
            Http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            await VerificarProgresso();
        }
        EstadoAtual = GameState.MainMenu;
    }

    private async Task VerificarProgresso()
    {
        try
        {
          var resposta = await Http.GetAsync("/api/game/current");
            if (resposta.IsSuccessStatusCode)
            {
                var currentGame = await resposta.Content.ReadFromJsonAsync<CurrentGameResponse>();
                TemJogoAtivo = currentGame.GameID.HasValue;
            }
        }
        catch { TemJogoAtivo = false; }
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
        await Http.PostAsync("/api/game/abandon", null);
        NovoJogo();
    }

    private void ContinuarJogo() { }
    private void NovoJogo() { }
    private void AbrirOpcoes() { }

    private void Sair()
    {
        SecureStorage.Default.Remove("auth_token");
        EstadoAtual = GameState.Autenticacao;
        TemJogoAtivo = false;
        MostrarAvisoNovoJogo = false;
    }

}
