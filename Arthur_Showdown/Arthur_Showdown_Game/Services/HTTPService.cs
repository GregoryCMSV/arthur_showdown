using Arthur_Showdown_Shared.Dtos.Game;
using Arthur_Showdown_Shared.Dtos.Usuario;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

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
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<bool> TentarRenovarTokenAsync()
        {
            try
            {
                var accessToken = await SecureStorage.Default.GetAsync("auth_token") ?? string.Empty;
                var refreshToken = await SecureStorage.Default.GetAsync("refresh_token");

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var respRefresh = await _httpClient.PostAsJsonAsync("/api/users/refresh", new RefreshTokenRequest
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshToken
                    });

                    if (respRefresh.IsSuccessStatusCode)
                    {
                        var novo = await respRefresh.Content.ReadFromJsonAsync<LoginResponse>();
                        if (novo != null && !string.IsNullOrEmpty(novo.Token))
                        {
                            await SecureStorage.Default.SetAsync("auth_token", novo.Token);
                            if (!string.IsNullOrEmpty(novo.RefreshToken))
                                await SecureStorage.Default.SetAsync("refresh_token", novo.RefreshToken);
                            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", novo.Token);
                            return true;
                        }
                    }
                }

                // Fallback caso o refresh token tenha expirado ou nao existisse antes
                var email = await SecureStorage.Default.GetAsync("auth_email");
                var senha = await SecureStorage.Default.GetAsync("auth_password");
                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(senha))
                {
                    var respLogin = await _httpClient.PostAsJsonAsync("/api/users/login", new { Email = email, Senha = senha });
                    if (respLogin.IsSuccessStatusCode)
                    {
                        var novo = await respLogin.Content.ReadFromJsonAsync<LoginResponse>();
                        if (novo != null && !string.IsNullOrEmpty(novo.Token))
                        {
                            await SecureStorage.Default.SetAsync("auth_token", novo.Token);
                            if (!string.IsNullOrEmpty(novo.RefreshToken))
                                await SecureStorage.Default.SetAsync("refresh_token", novo.RefreshToken);
                            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", novo.Token);
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<HttpResponseMessage> EnviarAutenticadoAsync(Func<Task<HttpResponseMessage>> acao)
        {
            await PrepararAutenticacaoAsync();
            var resposta = await acao();
            if (resposta.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (await TentarRenovarTokenAsync())
                {
                    resposta = await acao();
                }
            }
            return resposta;
        }

        public async Task<string?> GetCurrentGame()
        {
            try
            {
                var resposta = await EnviarAutenticadoAsync(() => _httpClient.GetAsync("/api/game/current"));
                if (resposta.IsSuccessStatusCode)
                {
                    var currentGame = await resposta.Content.ReadFromJsonAsync<CurrentGameResponse>();
                    return currentGame?.GameID?.ToString();
                }
                return null;
            }
            catch { return null; }
        }

        public async Task AbadonGame()
        {
            try
            {
                await EnviarAutenticadoAsync(() => _httpClient.PostAsync("/api/game/abandon", null));
            }
            catch { }
        }

        public async Task<int?> CriarGame(int personagemId)
        {
            try
            {
                var resposta = await EnviarAutenticadoAsync(() => _httpClient.PostAsJsonAsync("/api/game", new CriarGameRequest { PersonagemId = personagemId }));
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
                string url = resetCooldown ? $"/api/game/{gameId}/estado?resetCooldown=true" : $"/api/game/{gameId}/estado";
                var resposta = await EnviarAutenticadoAsync(() => _httpClient.GetAsync(url));
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
                var resposta = await EnviarAutenticadoAsync(() => _httpClient.PostAsJsonAsync($"/api/game/{gameId}/melhoria", new AplicarMelhoriaRequest
                {
                    DeckAtaquesId = deckAtaquesId,
                    Tipo = tipo
                }));
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
                var resposta = await EnviarAutenticadoAsync(() => _httpClient.GetAsync($"/api/game/{gameId}/opcoes-ataque"));
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
                var resposta = await EnviarAutenticadoAsync(() => _httpClient.PostAsJsonAsync($"/api/game/{gameId}/novo-ataque", new EscolherNovoAtaqueRequest
                {
                    AtaqueId = ataqueId
                }));
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
                var resposta = await EnviarAutenticadoAsync(() => _httpClient.PutAsJsonAsync($"/api/game/{gameId}/estado", estado));
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
                await EnviarAutenticadoAsync(() => _httpClient.PostAsync($"/api/game/{gameId}/finalizar?vitoria={vitoria.ToString().ToLower()}", null));
            }
            catch { }
        }

        public async Task<List<PersonagemDto>?> GetUnlockedCharacters()
        {
            try
            {
                var resposta = await EnviarAutenticadoAsync(() => _httpClient.GetAsync("/api/Personagem/Unlocked"));
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
