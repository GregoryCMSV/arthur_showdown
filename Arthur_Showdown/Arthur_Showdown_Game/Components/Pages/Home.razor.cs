using Arthur_Showdown_Game.Services;
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
    
    [Inject]
    private ImageService ImageService { get; set; }


    protected override async Task OnInitializedAsync()
    {
        var token = await SecureStorage.Default.GetAsync("auth_token");
        if (!string.IsNullOrEmpty(token))
        {
            EstadoAtual = GameState.MainMenu;
        }
    }

    private void TentarPassarDaTelaInicial()
    {
        if (EstadoAtual == GameState.PressAnyKey)
            EstadoAtual = GameState.Autenticacao;
    }

    private void IrParaMenuPrincipal()
    {
        EstadoAtual = GameState.MainMenu;
    }

    private void ContinuarJogo() { }
    private void NovoJogo() { }
    private void AbrirOpcoes() { }

    private void Sair()
    {
        SecureStorage.Default.Remove("auth_token");
        EstadoAtual = GameState.Autenticacao;
    }

}
