using Microsoft.Extensions.Logging;
using SnapBlast.Configuration;
using SnapBlast.Services;
using SnapBlast.ViewModels;

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

            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddSingleton<QuizApiClient>(_ =>
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(ApiSettings.BaseUrl)
                };
                return new QuizApiClient(client);
            });
            builder.Services.AddTransient<MainPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
