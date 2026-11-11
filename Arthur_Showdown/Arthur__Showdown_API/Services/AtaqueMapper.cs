using Arthur_Showdown_Database.Models.Base;
using Arthur_Showdown_Database.Models.Run;
using Arthur_Showdown_Shared.Dtos.Game;
using System;
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

        public static AtaqueDto ToDto(DeckAtaques d)
        {
            var a = d.Ataque!;
            return new AtaqueDto
            {
                Id = a.Id,
                Nome = a.Nome,
                Descricao = a.Descricao,
                ImagemUrl = a.ImagemUrl,
                Dano = Math.Max(0, a.Dano + d.ModDano),
                Cooldown = Math.Max(0, a.Cooldown + d.ModCooldown),
                IsFastAction = d.IsFastAction || a.IsFastAction,
                LimiteAprimoramento = d.LimiteUpgrade > 0 ? d.LimiteUpgrade : a.LimiteAprimoramento,
                HitboxJson = JsonSerializer.Serialize(a.Hitbox)
            };
        }
    }
}
