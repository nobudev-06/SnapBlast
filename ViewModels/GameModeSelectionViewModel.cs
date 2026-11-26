using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapBlast.Services;

namespace SnapBlast.ViewModels;

public partial class GameModeSelectionViewModel : ObservableObject
{
    private readonly QuizApiClient apiClient;

    [ObservableProperty]
    private string greeting = "Choose a game mode";

    public GameModeSelectionViewModel(QuizApiClient apiClient)
    {
        this.apiClient = apiClient;
        Refresh();
    }

    public void Refresh() =>
        Greeting = string.IsNullOrWhiteSpace(apiClient.DisplayName)
            ? "Choose a game mode"
            : $"Welcome, {apiClient.DisplayName}";

    // "//route" = absolute navigation to a ShellContent route declared in AppShell.xaml.
    [RelayCommand]
    private Task OpenBlockBlast() => Shell.Current.GoToAsync("//blockblast");

    [RelayCommand]
    private Task OpenMultipleChoice() => Shell.Current.GoToAsync("//multiplechoice");

    [RelayCommand]
    private async Task SignOut()
    {
        apiClient.SignOut();
        await Shell.Current.GoToAsync("//login");
    }
}