using Microsoft.Extensions.DependencyInjection;

namespace SnapBlast;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        var services = IPlatformApplication.Current?.Services;
        var page = services?.GetService<MainPage>()
            ?? new MainPage(services?.GetRequiredService<ViewModels.MainViewModel>()
                ?? throw new InvalidOperationException("SnapBlast services are not initialized."));
        Items.Add(new ShellContent
        {
            Title = "Home",
            Route = "MainPage",
            Content = page
        });
    }
}
