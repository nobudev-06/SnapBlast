using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapBlast.Services;

namespace SnapBlast.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly QuizApiClient _apiClient;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private bool isRegisterMode = false;

    [ObservableProperty]
    private bool isBusy = false;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public LoginViewModel(QuizApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    public void SwitchToLogin()
    {
        IsRegisterMode = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    public void SwitchToRegister()
    {
        IsRegisterMode = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    public async Task Authenticate()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Email and password are required.";
            return;
        }

        if (IsRegisterMode && string.IsNullOrWhiteSpace(DisplayName))
        {
            StatusMessage = "Display name is required for registration.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiClient.AuthenticateAsync(
                email: Email,
                password: Password,
                displayName: DisplayName,
                isRegister: IsRegisterMode);

            if (result.Success)
            {
                // Navigate to game mode selection
                await Shell.Current.GoToAsync("//gamemode_selection");
            }
            else
            {
                StatusMessage = result.Message ?? "Authentication failed.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
