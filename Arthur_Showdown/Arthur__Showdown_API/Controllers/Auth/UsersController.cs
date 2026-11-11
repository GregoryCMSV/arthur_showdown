using Arthur_Showdown_Database.Auth;
using Arthur_Showdown_Database.Data;
using Arthur_Showdown_Database.Models.Base;
using Arthur_Showdown_Shared.Dtos.Usuario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arthur__Showdown_API.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly SupabaseAuthManager _authManager;
        private readonly ArthurShowdownContext _db;
        private readonly ILogger<UsersController> _logger;

        public UsersController(SupabaseAuthManager authManager, ArthurShowdownContext db, ILogger<UsersController> logger)
        {
            _authManager = authManager;
            _db = db;
            _logger = logger;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarUsuario([FromBody] UserRegistrationRequest request)
        {
            try
            {
                _logger.LogInformation($"Registrando usuário: {request.Email}, Tipo: {request.TipoUsuario}");
                var session = await _authManager.RegistrarUsuario(request.Email, request.Senha, request.TipoUsuario, request.Nome);
                if (session?.User != null)
                {
                    _logger.LogInformation($"Usuário registrado | ID: {session.User.Id}");
                    var progresso = new ProgressoJogador
                    {
                        JogadorId = Guid.Parse(session.User.Id),
                        EmblemasKamelot = 0,
                        PersonagensDesbloqueadosIds = new List<int> { 1 },
                        AtaquesDesbloqueadosIds = new List<int>(),
                        HabilidadesDesbloqueadasIds = new List<int>()
                    };

                    _db.ProgressoJogadores.Add(progresso);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    _logger.LogInformation("Falha ao criar usuário no Supabase.");
                    return BadRequest("Falha ao criar usuário no Supabase.");
                }
                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"Erro ao registrar usuário: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation($"Fazendo login: {request.Email}");
                var session = await _authManager.FazerLogin(request.Email, request.Senha);
                if (session?.AccessToken == null) return Unauthorized("Credenciais inválidas.");

                _logger.LogInformation($"Login bem-sucedido | ID: {session.User?.Id}");
                return Ok(new LoginResponse
                {
                    Token = session.AccessToken,
                    RefreshToken = session.RefreshToken,
                    UsuarioId = session.User?.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao fazer login: {ex.Message}");
                return Unauthorized("Erro ao fazer login: " + ex.Message);
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return Unauthorized("Refresh token inválido.");

                var session = await _authManager.RenovarSessao(request.AccessToken, request.RefreshToken);
                if (session?.AccessToken == null)
                    return Unauthorized("Não foi possível renovar a sessão.");

                return Ok(new LoginResponse
                {
                    Token = session.AccessToken,
                    RefreshToken = session.RefreshToken,
                    UsuarioId = session.User?.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao renovar token: {ex.Message}");
                return Unauthorized("Erro ao renovar token: " + ex.Message);
            }
        }
    }
}
