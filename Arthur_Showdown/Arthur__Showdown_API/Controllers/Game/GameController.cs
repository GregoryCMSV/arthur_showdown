using Arthur__Showdown_API.Services;
using Arthur_Showdown_Database.Data;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Arthur__Showdown_API.Controllers.Game
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly ArthurShowdownContext _db;
        private readonly TokenService _tokenService;

        public GameController(ArthurShowdownContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentGame()
        {
            var userId = _tokenService.ObterUsuarioId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuário inválido no token.");
            }

            var jogoAtivo = await _db.Games
                .Where(j => j.JogadorId.ToString() == userId && j.DataFim == null)
                .Select(j => j.Id) 
                .FirstOrDefaultAsync();

            if (jogoAtivo == 0) 
            {
                return Ok(new CurrentGameResponse { GameID = null });
            }

            return Ok(new CurrentGameResponse { GameID = jogoAtivo });
        }

        [HttpPost("abandon")]
        public async Task<IActionResult> AbandonCurrentGame()
        {
            var userId = _tokenService.ObterUsuarioId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuário inválido no token.");
            }

            var jogoAtivo = await _db.Games
                .FirstOrDefaultAsync(j => j.JogadorId.ToString() == userId && j.DataFim == null);

            if (jogoAtivo == null)
            {
                return NotFound(new { Mensagem = "Nenhum jogo ativo encontrado para este usuário." });
            }

            
            jogoAtivo.DataInicio = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { Mensagem = "Jogo abandonado com sucesso." });
        }
    }
}
