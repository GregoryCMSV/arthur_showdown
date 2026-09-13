using Arthur__Showdown_API.Services;
using Arthur_Showdown_Database.Data;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Arthur__Showdown_API.Controllers.Game
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonagemController : ControllerBase
    {
        private readonly ArthurShowdownContext _db;
        private readonly TokenService _tokenService;

        public PersonagemController(ArthurShowdownContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        [HttpGet("Unlocked")]
        public async Task<IActionResult> GetUnlockedCharacters()
        {
            var userId = _tokenService.ObterUsuarioId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Usuário não autenticado.");

            // Flag: adicionar where para trazer só personagens desbloqueados

            var personagens = await _db.Personagens
                .Select(p => new PersonagemDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    ImagemUrl = p.ImagemUrl,
                    AtaquesIniciais = _db.PersonagemAtaques
                        .AsNoTracking()
                        .Where(pa => pa.PersonagemId == p.Id)
                        .Select(pa => new AtaqueDto
                        {
                            Id = pa.Ataque.Id,
                            Nome = pa.Ataque.Nome,
                            Cooldown = pa.Ataque.Cooldown,
                            Descricao = pa.Ataque.Descricao,
                            Dano = pa.Ataque.Dano,
                            IsFastAction = pa.Ataque.IsFastAction,
                            LimiteAprimoramento = pa.Ataque.LimiteAprimoramento,
                            ImagemUrl = pa.Ataque.ImagemUrl,
                            HitboxJson = pa.Ataque.Hitbox.ToString()
                        }).ToList()
                })
                .ToListAsync();

            return Ok(personagens);
        }
    }
}
