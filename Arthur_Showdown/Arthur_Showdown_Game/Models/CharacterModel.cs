using Arthur_Showdown_Game.Components.Game;
using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Models;

public class CharacterModel : IDisposable
{
    private readonly InputManager _inputManager;
    public event Action? OnChange;
    public PersonagemDto Personagem { get; set; }
    public int PosicaoPersonagem { get; set; } = -50;
    public int DirecaoOlhar { get; set; } = 1;
    public bool EstaEmMovimento { get; set; } = false;
    public bool EstaVirando { get; set; } = false;
    public int CategoriaPeso { get; set; } = 1;
    public bool ModoCombate { get; set; } = false;

    public CharacterModel(InputManager inputManager)
    {
        _inputManager = inputManager;
        _inputManager.AoSolicitarMovimento += ProcessarMovimento;
        _inputManager.AoSolicitarTrocaDirecao += ProcessarTrocaDirecao;
    }

    public async void ProcessarTrocaDirecao()
    {
        if (ModoCombate) return;
        await VirarAsync();
    }

    public async Task VirarAsync()
    {
   
        if (EstaEmMovimento) return;
        if (EstaVirando) return;
        EstaVirando = true;
        EstaEmMovimento = true;
        DirecaoOlhar *= -1;
        OnChange?.Invoke();
        await Task.Delay(150);
        EstaEmMovimento = false;
        EstaVirando = false;
        OnChange?.Invoke();
    }

    private async void ProcessarMovimento(int direcao)
    {
        if (ModoCombate) return;
        await MoverAsync(direcao);
    }

    public async Task MoverAsync(int direcao)
    {
        if (EstaEmMovimento) return;

        int novaPosicao =  PosicaoPersonagem + direcao;
        EstaEmMovimento = true;

        if (novaPosicao >= 0 && novaPosicao < 5)
        {
            PosicaoPersonagem = novaPosicao;
        }

        OnChange?.Invoke();
        await Task.Delay(150);
        EstaEmMovimento = false;
        OnChange?.Invoke();
    }

    public void Dispose()
    {
        _inputManager.AoSolicitarMovimento -= ProcessarMovimento;
        _inputManager.AoSolicitarTrocaDirecao -= ProcessarTrocaDirecao;
    }

    public List<AtaqueDto> GetAtaques()
    {
        return Personagem.AtaquesIniciais;
    }
}
