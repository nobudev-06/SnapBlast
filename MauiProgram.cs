using Microsoft.Extensions.Logging;
using SnapBlast.Configuration;
using SnapBlast.Services;
using SnapBlast.ViewModels;
using SnapBlast.Views;

namespace SnapBlast
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // One shared API client so the signed-in user/token is the same everywhere.
            builder.Services.AddSingleton<QuizApiClient>(_ =>
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(ApiSettings.BaseUrl)
                };
                return new QuizApiClient(client);
            });

            // Every page that Shell creates from a DataTemplate must be registered,
            // together with its view model, so constructor injection works.
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            builder.Services.AddTransient<GameModeSelectionViewModel>();
            builder.Services.AddTransient<GameModeSelectionPage>();

            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<MainPage>();

            builder.Services.AddTransient<MultipleChoiceViewModel>();
            builder.Services.AddTransient<MultipleChoiceGamePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}