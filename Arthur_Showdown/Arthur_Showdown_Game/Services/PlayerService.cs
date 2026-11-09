using Arthur_Showdown_Game.Models;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services;

public class PlayerService
{
    private HTTPService _httpService;
    private InputManager _inputManager;
    private List<PersonagemDto> _unlockedCharacters = new();
    public CharacterModel? CurrentCharacter { get; private set; }

    public PlayerService(HTTPService httpService, InputManager inputManager)
    {
        _httpService = httpService;
        _inputManager = inputManager;
    }

    public async Task LoadCharacters()
    {
        try
        {
            var resposta = await _httpService.GetUnlockedCharacters();
            if (resposta != null && resposta.Any())
            {
                _unlockedCharacters = resposta;
                CurrentCharacter = new CharacterModel(_inputManager)
                {
                    Personagem = _unlockedCharacters.First()
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar personagens: {ex.Message}");
        }
    }

    public void AdicionarPersonagem(PersonagemDto personagem)
    {
        if (!_unlockedCharacters.Exists(p => p.Id == personagem.Id))
        {
            _unlockedCharacters.Add(personagem);
        }
    }

    public void SetStartPosition(int startPosition)
    {
        CurrentCharacter?.PosicaoPersonagem = startPosition;
    }

    public PersonagemDto? GetCurrentCharacterDto()
    {
        return CurrentCharacter?.Personagem;
    }
}
