using Arthur_Showdown_Game.Services;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Arthur_Showdown_Shared.Dtos.Game;

namespace Arthur_Showdown_Game.Components.Pages;

public partial class GameScreen : ComponentBase, IDisposable
{
    [Parameter] public int? GameId { get; set; }
    [Inject] public HttpClient Http { get; set; } = default!;

    private bool IsLobby => !GameId.HasValue;
    private string JanelaAberta = "";

    // --- Estado da Arena ---
    private int PosicaoPersonagem = 2;
    private bool EstaEmMovimento = false;
    private int TileTremendoId = -1;

    // --- Dados do Jogador ---
    private List<PersonagemDto> PersonagensDesbloqueados = new();
    private PersonagemDto? PersonagemAtual;

    private InputManager Controle = new();

    protected override async Task OnInitializedAsync()
    {
        var token = await SecureStorage.Default.GetAsync("auth_token");
        if (!string.IsNullOrEmpty(token))
        {
            Http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        await CarregarPersonagens();
        Controle.AoSolicitarMovimento += ProcessarMovimento;

        if (!IsLobby) await CarregarSaveData(GameId);
    }

    private async Task CarregarPersonagens()
    {
        try
        {
            var resposta = await Http.GetFromJsonAsync<List<PersonagemDto>>("/api/Personagem/Unlocked");
            if (resposta != null && resposta.Any())
            {
                PersonagensDesbloqueados = resposta;
                PersonagemAtual = PersonagensDesbloqueados.First();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar personagens: {ex.Message}");
        }
    }

    private async Task CarregarSaveData(int? id)
    {
        Console.WriteLine($"Restaurando estado do Jogo: {id}");
    }

    private void AbrirMapa()
    {
        JanelaAberta = "Mapa";
        StateHasChanged();
    }

    private async void ProcessarMovimento(int direcao)
    {
        if (EstaEmMovimento) return;
        if (!string.IsNullOrEmpty(JanelaAberta) && JanelaAberta != "Mapa")
        {
            JanelaAberta = "";
        }
        else if (JanelaAberta == "Mapa")
        {
            JanelaAberta = "";
            StateHasChanged();
            return;
        }

        int novaPosicao = PosicaoPersonagem + direcao;
        if (novaPosicao >= 0 && novaPosicao < 5)
        {
            EstaEmMovimento = true;
            PosicaoPersonagem = novaPosicao;
            StateHasChanged();

            await Task.Delay(150); 
            ProcessarEventosDoTile(PosicaoPersonagem);

            EstaEmMovimento = false;
            StateHasChanged();
        }
    }

    private async void ProcessarEventosDoTile(int posicao)
    {
        if (!IsLobby) return;

        switch (posicao)
        {
            case 0:
                JanelaAberta = "Loja";
                break;
            case 1:
                JanelaAberta = "Scoreboard";
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
        Controle.AoSolicitarMovimento -= ProcessarMovimento;
    }
}