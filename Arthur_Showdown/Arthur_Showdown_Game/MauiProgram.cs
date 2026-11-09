using Arthur_Showdown_Game.Services;
using Microsoft.Extensions.Logging;

namespace Arthur_Showdown_Game
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://arthur-showdown-api.onrender.com") });
            builder.Services.AddSingleton<ImageService>();
            builder.Services.AddSingleton<HTTPService>();
            builder.Services.AddScoped<NavigationService>();
            builder.Services.AddScoped<InputManager>();
            builder.Services.AddScoped<PlayerService>();
            builder.Services.AddScoped<EnemiesService>();
            builder.Services.AddScoped<LoadingService>();
            builder.Services.AddScoped<TurnManager>();
            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
