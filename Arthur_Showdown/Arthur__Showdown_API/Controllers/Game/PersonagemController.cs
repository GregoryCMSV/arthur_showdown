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

            var personagensDb = await _db.Personagens.AsNoTracking().ToListAsync();
            var vinculos = await _db.PersonagemAtaques
                .AsNoTracking()
                .Include(pa => pa.Ataque)
                .ToListAsync();

            var personagens = personagensDb.Select(p => new PersonagemDto
            {
                Id = p.Id,
                Nome = p.Nome,
                ImagemUrl = p.ImagemUrl,
                AtaquesIniciais = vinculos
                    .Where(pa => pa.PersonagemId == p.Id)
                    .Select(pa => AtaqueMapper.ToDto(pa.Ataque!))
                    .ToList()
            }).ToList();

            return Ok(personagens);
        }
    }
}
