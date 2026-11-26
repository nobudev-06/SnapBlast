using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapBlast.Models;
using SnapBlast.Services;
using System.Collections.ObjectModel;

namespace SnapBlast.ViewModels;

public enum GameStage
{
    Setup,
    Playing,
    Finished
}

public partial class MultipleChoiceViewModel : ObservableObject
{
    private readonly QuizApiClient apiClient;
    private List<MultipleChoiceQuestion> questions = [];
    private int currentIndex;

    public MultipleChoiceViewModel(QuizApiClient apiClient)
    {
        this.apiClient = apiClient;
        StatusMessage = "Choose how to build your quiz.";
    }

    public ObservableCollection<AnswerOptionViewModel> Options { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetupVisible))]
    [NotifyPropertyChangedFor(nameof(IsPlayingVisible))]
    [NotifyPropertyChangedFor(nameof(IsFinishedVisible))]
    private GameStage stage = GameStage.Setup;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string quizTitle = string.Empty;
    [ObservableProperty] private string questionText = string.Empty;
    [ObservableProperty] private string progressText = string.Empty;
    [ObservableProperty] private string feedbackText = string.Empty;
    [ObservableProperty] private string nextButtonText = "Next question";
    [ObservableProperty] private string resultText = string.Empty;
    [ObservableProperty] private int score;
    [ObservableProperty] private bool isAnswered;

    public bool IsSetupVisible => Stage == GameStage.Setup;
    public bool IsPlayingVisible => Stage == GameStage.Playing;
    public bool IsFinishedVisible => Stage == GameStage.Finished;
    public bool IsNotBusy => !IsBusy;

    // ---------- Building a quiz ----------

    [RelayCommand]
    private void UseSampleQuiz()
    {
        BeginQuiz("Networking basics (sample)",
        [
            new("What is the purpose of DNS?", "Translates domain names into IP addresses",
                ["Encrypts traffic between two devices", "Assigns IP addresses automatically to new devices", "Forwards data packets between networks"]),
            new("Which device forwards data packets between networks?", "Router",
                ["Firewall", "Repeater", "DNS server"]),
            new("What does a firewall do?", "Filters network traffic according to security rules",
                ["Translates domain names into IP addresses", "Stores web pages for faster loading", "Boosts the wireless signal"]),
            new("HTTP is a protocol used to transfer what?", "Web resources such as pages and images",
                ["Email attachments only", "Encrypted database backups", "Raw hardware drivers"])
        ]);
    }

    [RelayCommand]
    private async Task ImportDocument()
    {
        if (IsBusy) return;
        try
        {
            var file = await StudyFilePicker.PickAsync();
            if (file is null) return;
            await GenerateFromFile(file);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not open the file picker: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task CaptureDocument()
    {
        if (IsBusy) return;
        try
        {
            var file = await StudyFilePicker.CaptureAsync();
            if (file is null) return;
            await GenerateFromFile(file);
        }
        catch (PermissionException)
        {
            StatusMessage = "Camera permission was denied. Enable camera access in the device settings and try again.";
        }
        catch (FeatureNotSupportedException)
        {
            StatusMessage = "Camera capture is not available on this device. Use Import image or PDF instead.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not open the camera: {exception.Message}";
        }
    }

    private async Task GenerateFromFile(FileResult file)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Sending the document to Google AI for text extraction and question writing...";

            var generated = await apiClient.GenerateMultipleChoiceAsync(file, null, CancellationToken.None);
            if (generated is null || generated.Blocks.Count == 0)
            {
                StatusMessage = "The AI returned no questions. Try a clearer photo or a different document.";
                return;
            }

            var built = new List<MultipleChoiceQuestion>();
            foreach (var block in generated.Blocks)
            {
                var question = block.Term.Trim();
                var answer = block.Definition.Trim();
                if (question.Length == 0 || answer.Length == 0) continue;

                var wrong = block.Distractors
                    .Select(item => item.Trim())
                    .Where(item => item.Length > 0 && !item.Equals(answer, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

                if (wrong.Count > 0)
                {
                    built.Add(new MultipleChoiceQuestion(question, answer, wrong));
                }
            }

            if (built.Count == 0)
            {
                StatusMessage = "The AI response had no usable questions. Please try again.";
                return;
            }

            BeginQuiz(string.IsNullOrWhiteSpace(generated.Title) ? "Generated quiz" : generated.Title, built);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Quiz generation failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------- Playing ----------

    private void BeginQuiz(string title, IEnumerable<MultipleChoiceQuestion> source)
    {
        questions = source.OrderBy(_ => Random.Shared.Next()).ToList();
        QuizTitle = title;
        Score = 0;
        currentIndex = 0;
        Stage = GameStage.Playing;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        var question = questions[currentIndex];
        QuestionText = question.Question;
        ProgressText = $"{currentIndex + 1} of {questions.Count}";
        NextButtonText = currentIndex + 1 >= questions.Count ? "See results" : "Next question";
        FeedbackText = string.Empty;
        IsAnswered = false;

        var choices = question.WrongAnswers
            .Select(text => (Text: text, IsCorrect: false))
            .Append((Text: question.CorrectAnswer, IsCorrect: true))
            .ToArray();
        Random.Shared.Shuffle(choices);

        Options.Clear();
        for (var index = 0; index < choices.Length; index++)
        {
            Options.Add(new AnswerOptionViewModel((char)('A' + index), choices[index].Text, choices[index].IsCorrect, OnOptionSelected));
        }
    }

    private void OnOptionSelected(AnswerOptionViewModel selected)
    {
        if (IsAnswered) return; // ignore extra taps once answered
        IsAnswered = true;

        foreach (var option in Options)
        {
            if (option.IsCorrect) option.ShowCorrect();
            else if (ReferenceEquals(option, selected)) option.ShowWrong();
        }

        if (selected.IsCorrect)
        {
            Score++;
            FeedbackText = "Correct!";
        }
        else
        {
            FeedbackText = $"Not quite. The correct answer is: {questions[currentIndex].CorrectAnswer}";
        }
    }

    [RelayCommand]
    private void NextQuestion()
    {
        if (!IsAnswered) return;

        if (currentIndex + 1 >= questions.Count)
        {
            var percent = questions.Count == 0 ? 0 : Score * 100 / questions.Count;
            ResultText = $"You got {Score} of {questions.Count} correct ({percent}%).";
            Stage = GameStage.Finished;
            return;
        }

        currentIndex++;
        ShowQuestion();
    }

    [RelayCommand]
    private void Restart() => BeginQuiz(QuizTitle, questions);

    [RelayCommand]
    private void NewQuiz()
    {
        StatusMessage = "Choose how to build your quiz.";
        Stage = GameStage.Setup;
    }

    [RelayCommand]
    private Task BackToMenu() => Shell.Current.GoToAsync("//gamemode_selection");
}