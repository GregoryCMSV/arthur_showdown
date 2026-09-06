using Arthur_Showdown_Database.Auth;
using Arthur_Showdown_Database.Data;
using Arthur_Showdown_Database.Models.Base;
using Arthur_Showdown_Shared.Dtos.Usuario;
using Microsoft.AspNetCore.Mvc;

namespace Arthur__Showdown_API.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        SupabaseAuthManager _authManager;
        ArthurShowdownContext _db;

        public UsersController(SupabaseAuthManager authManager, ArthurShowdownContext db)
        {
            _authManager = authManager;
            _db = db;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarUsuario([FromBody] UserRegistrationRequest request)
        {
            try
            {
                var session = await _authManager.RegistrarUsuario(request.Email, request.Senha, request.TipoUsuario, request.Nome);
                if (session?.User != null)
                {
                    
                    var progresso = new ProgressoJogador
                    {
                        JogadorId = Guid.Parse(session.User.Id),
                        EmblemasKamelot = 0,
                        PersonagensDesbloqueadosIds = new List<int> { 0 },
                        AtaquesDesbloqueadosIds = new List<int>(),
                        HabilidadesDesbloqueadasIds = new List<int>()
                    };

                    _db.ProgressoJogadores.Add(progresso);
                    await _db.SaveChangesAsync(); 
                }
                else
                {
                    return BadRequest("Falha ao criar usuário no Supabase.");
                }
                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var session = await _authManager.FazerLogin(request.Email, request.Senha);
                if (session?.AccessToken == null) return Unauthorized("Credenciais inválidas.");

                return Ok(new { Token = session.AccessToken, UsuarioId = session.User?.Id });
            } catch (Exception ex) {
                return Unauthorized("Erro ao fazer login: " + ex.Message);
            }
        }
    }
}
