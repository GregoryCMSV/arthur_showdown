
using Arthur_Showdown_Database.Auth;
using Arthur_Showdown_Database.Data;
using Microsoft.EntityFrameworkCore;

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

            builder.Services.AddScoped<Supabase.Client>(_ =>
                new Supabase.Client(supabaseUrl, supabaseKey));
            builder.Services.AddScoped<SupabaseAuthManager>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
