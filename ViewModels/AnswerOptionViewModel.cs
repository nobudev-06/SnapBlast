using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SnapBlast.ViewModels;
public partial class AnswerOptionViewModel : ObservableObject
{
    private static readonly Color Neutral = Color.FromArgb("#E8DFF5");
    private static readonly Color Correct = Color.FromArgb("#C9EBDD");
    private static readonly Color Wrong = Color.FromArgb("#F4B6B6");

    private readonly Action<AnswerOptionViewModel> onSelected;

    public AnswerOptionViewModel(char letter, string text, bool isCorrect, Action<AnswerOptionViewModel> onSelected)
    {
        DisplayText = $"{letter}.  {text}";
        IsCorrect = isCorrect;
        this.onSelected = onSelected;
    }

    public string DisplayText { get; }
    public bool IsCorrect { get; }

    [ObservableProperty]
    private Color fillColor = Neutral;

    [RelayCommand]
    private void Select() => onSelected(this);

    public void ShowCorrect() => FillColor = Correct;
    public void ShowWrong() => FillColor = Wrong;
}