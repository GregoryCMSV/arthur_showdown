using Arthur_Showdown_Database.Models.Auxiliar;
using Arthur_Showdown_Database.Models.Base;
using Arthur_Showdown_Database.Models.Run;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Database.Data
{
    public class ArthurShowdownContext : DbContext
    {
        public ArthurShowdownContext(DbContextOptions<ArthurShowdownContext> options) : base(options)
        {
        }
        #region DBSets
        // Base
        public DbSet<Personagem> Personagens { get; set; }
        public DbSet<Ataque> Ataques { get; set; }
        public DbSet<Item> Itens { get; set; }
        public DbSet<Habilidade> Habilidades { get; set; }
        public DbSet<Local> Locais { get; set; }
        public DbSet<ProgressoJogador> ProgressoJogadores { get; set; }

        // Auxiliar
        public DbSet<TipoPersonagem> TiposPersonagem { get; set; }
        public DbSet<Efeito> Efeitos { get; set; }
        public DbSet<StatusRun> StatusRuns { get; set; }
        public DbSet<Sessao> Sessoes { get; set; }
        public DbSet<Wave> Waves { get; set; }

        // Run
        public DbSet<Game> Games { get; set; }
        public DbSet<PersonagemRun> PersonagensRun { get; set; }
        public DbSet<DeckAtaques> DecksAtaques { get; set; }
        public DbSet<Inventario> Inventarios { get; set; }
        public DbSet<HabilidadesRun> HabilidadesRun { get; set; }
        public DbSet<WaveAtual> WavesAtuais { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Personagem>()
                .HasOne(p => p.TipoPersonagem)
                .WithMany(t => t.Personagens)
                .HasForeignKey(p => p.TipoPersonagemId);

            // Mapear o Hitbox como JSON
            modelBuilder.Entity<Ataque>()
                .OwnsOne(a => a.Hitbox, b =>
                {
                    b.ToJson();
                });

            modelBuilder.Entity<WaveAtual>()
            .HasOne(w => w.Game)
            .WithMany()
            .HasForeignKey(w => w.GameId);

            modelBuilder.Entity<Game>()
            .HasOne(g => g.WaveAtual)
            .WithMany()
            .HasForeignKey(g => g.WaveAtualId);

            modelBuilder.Entity<ProgressoJogador>()
            .HasKey(p => p.JogadorId);

            modelBuilder.Entity<TipoPersonagem>().HasData(
                new TipoPersonagem { Id = 1, Nome = "Playable" },
                new TipoPersonagem { Id = 2, Nome = "Enemy" },
                new TipoPersonagem { Id = 3, Nome = "Npc" }
            );

            modelBuilder.Entity<StatusRun>().HasData(
                new StatusRun { Id = 1, Nome = "Em Andamento" },
                new StatusRun { Id = 2, Nome = "Vitoria" },
                new StatusRun { Id = 3, Nome = "Derrota" },
                new StatusRun { Id = 4, Nome = "Desistencia" }
            );
        }
    }
}
