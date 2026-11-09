using Arthur_Showdown_Database.Models.Base;
using Arthur_Showdown_Shared.Dtos.Game;
using System.Text.Json;

namespace Arthur__Showdown_API.Services
{
    public static class AtaqueMapper
    {
        public static AtaqueDto ToDto(Ataque a) => new()
        {
            Id = a.Id,
            Nome = a.Nome,
            Descricao = a.Descricao,
            ImagemUrl = a.ImagemUrl,
            Dano = a.Dano,
            Cooldown = a.Cooldown,
            IsFastAction = a.IsFastAction,
            LimiteAprimoramento = a.LimiteAprimoramento,
            HitboxJson = JsonSerializer.Serialize(a.Hitbox)
        };
    }
}
