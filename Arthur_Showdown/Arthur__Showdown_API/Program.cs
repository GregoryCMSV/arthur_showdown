using Arthur__Showdown_API.Services;
using Arthur_Showdown_Database.Auth;
using Arthur_Showdown_Database.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Arthur__Showdown_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var supabaseUrl = builder.Configuration["SupabaseUrl"];
            var supabaseKey = builder.Configuration["SupabaseAnonKey"];
            builder.Services.AddDbContext<ArthurShowdownContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<Supabase.Client>(_ =>
                new Supabase.Client(supabaseUrl, supabaseKey));
            builder.Services.AddScoped<SupabaseAuthManager>();
            builder.Services.AddScoped<TokenService>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var jwtSecret = @"{
                ""x"": ""VY4U9o0tnybvmyYvAsREUwilNNiHpm7g6K9BclDTia4"",
                ""y"": ""jHqLMvLSxY1mcovGEvrHEniCN9Psa51bd4Upo6mjC80"",
                ""alg"": ""ES256"",
                ""crv"": ""P-256"",
                ""ext"": true,
                ""kid"": ""7c35fd78-cbe5-4fa4-a519-4095aac5563f"",
                ""kty"": ""EC"",
                ""key_ops"": [ ""verify"" ]
            }";

            var ecdsaKey = new JsonWebKey(jwtSecret);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
            {
                options.Authority = "https://cqzgfbtcmybebeqpomgv.supabase.co/auth/v1";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = ecdsaKey,
                    ValidAudience = "authenticated",
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidateLifetime = true
                };
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ArthurShowdownContext>();
                db.Database.Migrate();
                db.Database.ExecuteSqlRaw(@"
                    INSERT INTO ""Efeitos"" (""Id"", ""Nome"", ""Descricao"")
                    VALUES
                        (1, 'Maldição', 'Amaldiçoa o alvo, fazendo ele receber o dobro de dano do próximo ataque'),
                        (2, 'Espelho', 'Vira o alvo para o lado contrário'),
                        (3, 'Escudo', 'Ignora o primeiro dano recebido')
                    ON CONFLICT (""Id"") DO UPDATE SET
                        ""Nome"" = EXCLUDED.""Nome"",
                        ""Descricao"" = EXCLUDED.""Descricao"";

                    UPDATE ""Ataques"" SET ""EfeitoId"" = 3 WHERE ""Id"" = 2 OR ""Nome"" = 'Escudo';

                    INSERT INTO ""Ataques"" (""Nome"", ""Descricao"", ""Dano"", ""Cooldown"", ""IsFastAction"", ""LimiteAprimoramento"", ""EfeitoId"", ""Hitbox"", ""ImagemUrl"")
                    SELECT v.""Nome"", v.""Descricao"", v.""Dano"", v.""Cooldown"", v.""IsFastAction"", 3, v.""EfeitoId"", v.""Hitbox""::jsonb, v.""ImagemUrl""
                    FROM (VALUES
                        ('Ataque duplo', 'Ataca os espaços adjacentes', 2, 3, false, NULL::int, '{""Origem"": 0, ""Direcao"": 2, ""AlcanceMin"": 1, ""AlcanceMax"": 1}', 'https://shogunshowdown.wiki.gg/images/Tiles_swirl.png?16666f&20240924201144'),
                        ('Raio', 'Dispara um raio no inimigo mais distante', 3, 4, false, NULL::int, '{""Origem"": 0, ""Direcao"": 0, ""AlcanceMin"": 1, ""AlcanceMax"": 99}', 'https://shogunshowdown.wiki.gg/images/Tiles_lightning.png?a16c74&20240924201128'),
                        ('Shuriken', 'Acerta o primeiro alvo a frente', 1, 3, false, NULL::int, '{""Origem"": 0, ""Direcao"": 0, ""AlcanceMin"": 1, ""AlcanceMax"": 99}', 'https://shogunshowdown.wiki.gg/images/Tiles_shuriken.png?47f47b&20240924201137'),
                        ('Bomba de fumaça', 'Troca de posição com o alvo a frente', 0, 5, true, NULL::int, '{""Origem"": 0, ""Direcao"": 0, ""AlcanceMin"": 1, ""AlcanceMax"": 1}', 'https://shogunshowdown.wiki.gg/images/Tiles_smokeBomb.png?cac86d&20240924201139'),
                        ('Bomba de fumaça costas', 'Troca de posição com o alvo atrás', 0, 5, true, NULL::int, '{""Origem"": 0, ""Direcao"": 1, ""AlcanceMin"": 1, ""AlcanceMax"": 1}', 'https://shogunshowdown.wiki.gg/images/Tiles_back_Smoke_Bom2.png?2bf901'),
                        ('Amaldiçoar', 'Amaldiçoa o alvo a frente, fazendo ele receber o dobro de dano do próximo ataque', 0, 5, true, 1, '{""Origem"": 0, ""Direcao"": 0, ""AlcanceMin"": 1, ""AlcanceMax"": 1}', 'https://shogunshowdown.wiki.gg/images/Tiles_mark.png?411e2e&20240924201130'),
                        ('Virar', 'Vira para o lado oposto', 0, 3, true, NULL::int, '{""Origem"": 0, ""Direcao"": 0, ""AlcanceMin"": 0, ""AlcanceMax"": 0}', 'https://shogunshowdown.wiki.gg/images/Tile_Sharp_Turn.png?81ae49&20240924201515'),
                        ('Ataque nas costas', 'Ataca o alvo diretamente atrás', 3, 3, false, NULL::int, '{""Origem"": 0, ""Direcao"": 1, ""AlcanceMin"": 1, ""AlcanceMax"": 1}', 'https://shogunshowdown.wiki.gg/images/Tiles_backStrike.png?80b1a6&20240924201109'),
                        ('Espelho do lago', 'Vira todos os inimigos para o lado contrário', 0, 5, true, 2, '{""Origem"": 0, ""Direcao"": 2, ""AlcanceMin"": 1, ""AlcanceMax"": 99}', 'https://shogunshowdown.wiki.gg/images/RainOfMirrors.png?71e858')
                    ) AS v(""Nome"", ""Descricao"", ""Dano"", ""Cooldown"", ""IsFastAction"", ""EfeitoId"", ""Hitbox"", ""ImagemUrl"")
                    WHERE NOT EXISTS (SELECT 1 FROM ""Ataques"" a WHERE LOWER(a.""Nome"") = LOWER(v.""Nome""));

                    UPDATE ""Personagens"" SET ""Nome"" = 'Esqueleto', ""Vida"" = 3 WHERE ""Nome"" = 'InimigoGenerico' OR (""Nome"" = 'Esqueleto' AND ""Vida"" <= 0);
                    UPDATE ""Locais"" SET ""InimigosPossiveisIds"" = ARRAY(SELECT ""Id"" FROM ""Personagens"" WHERE ""Nome"" IN ('Esqueleto', 'Arqueiro', 'Lanceiro') ORDER BY ""Id"") WHERE ""Nome"" = 'Floresta';
                ");
            }

            // Configure the HTTP request pipeline.
           // if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger(); 
                app.UseSwaggerUI();
            //}

            //app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
