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
