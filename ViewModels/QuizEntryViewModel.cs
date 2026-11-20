using CommunityToolkit.Mvvm.ComponentModel;

namespace SnapBlast.ViewModels;

public partial class QuizEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string term = string.Empty;

    [ObservableProperty]
    private string definition = string.Empty;
}