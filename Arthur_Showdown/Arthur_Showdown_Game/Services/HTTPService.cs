using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using static System.Net.WebRequestMethods;

namespace Arthur_Showdown_Game.Services
{
    public class HTTPService
    {
        private readonly HttpClient _httpClient;

        public HTTPService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task PrepararAutenticacaoAsync()
        {
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<string?> GetCurrentGame()
        {
            try
            {
                await PrepararAutenticacaoAsync();
                var resposta = await _httpClient.GetAsync("/api/game/current");
                if (resposta.IsSuccessStatusCode)
                {
                    var currentGame = await resposta.Content.ReadFromJsonAsync<CurrentGameResponse>();
                    return currentGame.GameID?.ToString() ?? null;
                }
                return null;
            }
            catch { return null; }
        }

        public async Task AbadonGame()
        {
            try
            {
                await PrepararAutenticacaoAsync();
                var resposta = await _httpClient.PostAsync("/api/game/abandon", null);
                if (resposta.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }
        }

       
        public async Task<int?> CriarGame(int personagemId)
        {
            try
            {
                await PrepararAutenticacaoAsync();
                var resposta = await _httpClient.PostAsJsonAsync("/api/game", new CriarGameRequest { PersonagemId = personagemId });
                if (resposta.IsSuccessStatusCode)
                {
                    var criado = await resposta.Content.ReadFromJsonAsync<CriarGameResponse>();
                    return criado?.GameId;
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<EstadoJogoDto?> GetEstado(int gameId, bool resetCooldown = false)
        {
            try
            {
                await PrepararAutenticacaoAsync();
                string url = resetCooldown ? $"/api/game/{gameId}/estado?resetCooldown=true" : $"/api/game/{gameId}/estado";
                var resposta = await _httpClient.GetAsync(url);
                if (resposta.IsSuccessStatusCode)
                    return await resposta.Content.ReadFromJsonAsync<EstadoJogoDto>();
                return null;
            }
            catch { return null; }
        }

        public async Task<EstadoJogoDto?> AplicarMelhoria(int gameId, int deckAtaquesId, TipoMelhoriaAtaque tipo)
        {
            try
            {
                await PrepararAutenticacaoAsync();
                var resposta = await _httpClient.PostAsJsonAsync($"/api/game/{gameId}/melhoria", new AplicarMelhoriaRequest
                {
                    DeckAtaquesId = deckAtaquesId,
                    Tipo = tipo
                });
                if (resposta.IsSuccessStatusCode)
                    return await resposta.Content.ReadFromJsonAsync<EstadoJogoDto>();
                return null;
            }
            catch { return null; }
        }

        public async Task<List<AtaqueDto>?> ObterOpcoesNovoAtaque(int gameId)
        {
            try
            {
                await PrepararAutenticacaoAsync();
                var resposta = await _httpClient.GetAsync($"/api/game/{gameId}/opcoes-ataque");
                if (resposta.IsSuccessStatusCode)
                    return await resposta.Content.ReadFromJsonAsync<List<AtaqueDto>>();
                return null;
            }
            catch { return null; }
        }

        public async Task<EstadoJogoDto?> EscolherNovoAtaque(int gameId, int ataqueId)
        {
            try
            {
                await PrepararAutenticacaoAsync();
                var resposta = await _httpClient.PostAsJsonAsync($"/api/game/{gameId}/novo-ataque", new EscolherNovoAtaqueRequest
                {
                    AtaqueId = ataqueId
                });
                if (resposta.IsSuccessStatusCode)
                    return await resposta.Content.ReadFromJsonAsync<EstadoJogoDto>();
                return null;
            }
            catch { return null; }
        }

        public async Task<EstadoJogoDto?> SalvarEstado(int gameId, EstadoJogoDto estado)
        {
            try
            {
                await PrepararAutenticacaoAsync();
                var resposta = await _httpClient.PutAsJsonAsync($"/api/game/{gameId}/estado", estado);
                if (resposta.IsSuccessStatusCode)
                    return await resposta.Content.ReadFromJsonAsync<EstadoJogoDto>();
                return null;
            }
            catch { return null; }
        }

        public async Task FinalizarGame(int gameId, bool vitoria)
        {
            try
            {
                await PrepararAutenticacaoAsync();
                await _httpClient.PostAsync($"/api/game/{gameId}/finalizar?vitoria={vitoria.ToString().ToLower()}", null);
            }
            catch { }
        }

        public async Task<List<PersonagemDto>?> GetUnlockedCharacters()
        {
            try
            {
                await PrepararAutenticacaoAsync();
                var resposta = await _httpClient.GetAsync("/api/Personagem/Unlocked");
                if (resposta.IsSuccessStatusCode)
                {
                    return await resposta.Content.ReadFromJsonAsync<List<PersonagemDto>>();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
