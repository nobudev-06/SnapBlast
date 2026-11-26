using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapBlast.Configuration;
using SnapBlast.Services;

namespace SnapBlast.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly QuizApiClient apiClient;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubmitText))]
    private bool isRegisterMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string statusMessage = string.Empty;

    // Computed properties the XAML binds to, so the page needs no value converters.
    public string SubmitText => IsRegisterMode ? "Create account" : "Sign in";
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool IsNotBusy => !IsBusy;

    public LoginViewModel(QuizApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    [RelayCommand]
    private void SwitchToLogin()
    {
        IsRegisterMode = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void SwitchToRegister()
    {
        IsRegisterMode = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task Authenticate()
    {
        if (IsBusy) return;

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

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            await apiClient.AuthenticateAsync(Email.Trim(), Password, DisplayName.Trim(), IsRegisterMode, CancellationToken.None);

            Password = string.Empty;
            await Shell.Current.GoToAsync("//gamemode_selection");
        }
        catch (HttpRequestException exception)
        {
            StatusMessage = $"Could not reach the API at {ApiSettings.BaseUrl}. {exception.Message}";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
