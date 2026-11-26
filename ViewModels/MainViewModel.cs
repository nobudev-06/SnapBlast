using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapBlast.Game;
using SnapBlast.Models;
using SnapBlast.Services;

namespace SnapBlast.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private Quiz quiz = new()
    {
        Title = "Networking basics",
        Blocks =
        [
            new GameBlock { Term = "HTTP", Definition = "A protocol used to transfer web resources." },
            new GameBlock { Term = "DNS", Definition = "Translates domain names into IP addresses." },
            new GameBlock { Term = "IP address", Definition = "A numerical address that identifies a device on a network." },
            new GameBlock { Term = "Firewall", Definition = "Filters network traffic according to security rules." },
            new GameBlock { Term = "Router", Definition = "Forwards data packets between networks." }
        ]
    };

    private readonly PuzzleEngine engine = new();
    private readonly QuizApiClient apiClient;

    public ObservableCollection<QuizEntryViewModel> QuizEntries { get; } =
    [
        new QuizEntryViewModel(),
        new QuizEntryViewModel(),
        new QuizEntryViewModel(),
        new QuizEntryViewModel(),
        new QuizEntryViewModel()
    ];

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private int score;

    [ObservableProperty]
    private int matchedPairs;

    [ObservableProperty]
    private int combo;

    [ObservableProperty]
    private string matchFeedback = string.Empty;

    [ObservableProperty]
    private string customQuizTitle = string.Empty;

    [ObservableProperty]
    private bool isEditorVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    private int selectedTileIndex = -1;

    [ObservableProperty]
    private int boardVersion;

    public bool IsNotBusy => !IsBusy;

    public MainViewModel(QuizApiClient apiClient)
    {
        this.apiClient = apiClient;
        Title = quiz.Title;
        Tiles = [];
        CustomQuizTitle = quiz.Title;
        for (var index = 0; index < QuizEntries.Count && index < quiz.Blocks.Count; index++)
        {
            QuizEntries[index].Term = quiz.Blocks[index].Term;
            QuizEntries[index].Definition = quiz.Blocks[index].Definition;
        }
        StartNewGame();
    }

    public PuzzleEngine Engine => engine;
    public ObservableCollection<PuzzleTile> Tiles { get; }

    [RelayCommand]
    private Task BackToMenu() => Shell.Current.GoToAsync("//gamemode_selection");

    [RelayCommand]
    private void ToggleQuizEditor() => IsEditorVisible = !IsEditorVisible;

    [RelayCommand]
    private async Task ImportQuizFromImage()
    {
        if (IsBusy) return;
        try
        {
            var file = await StudyFilePicker.PickAsync();
            if (file is null)
            {
                return;
            }

            await GenerateQuizFromDocument(file);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not open the file picker: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task CaptureQuizImage()
    {
        if (IsBusy) return;
        try
        {
            var file = await StudyFilePicker.CaptureAsync();
            if (file is null)
            {
                return;
            }

            await GenerateQuizFromDocument(file);
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

    private async Task GenerateQuizFromDocument(FileResult file)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Sending the document to Google AI for text extraction and quiz generation...";

            // Title is null so the AI picks one from the document
            // (CustomQuizTitle defaults to "Networking basics", which would be forced onto every quiz).
            var generated = await apiClient.GenerateQuizAsync(file, null, CancellationToken.None);
            if (generated is null || generated.Blocks.Count == 0)
            {
                StatusMessage = "The AI returned no quiz pairs. You can enter the pairs manually.";
                IsEditorVisible = true;
                return;
            }

            CustomQuizTitle = generated.Title;
            QuizEntries.Clear();
            foreach (var block in generated.Blocks.Take(32))
            {
                QuizEntries.Add(new QuizEntryViewModel
                {
                    Term = block.Term,
                    Definition = block.Definition
                });
            }

            StatusMessage = $"Generated {generated.Blocks.Count} quiz pairs. Review them before starting the game.";
            IsEditorVisible = true;
        }
        catch (Exception exception)
        {
            StatusMessage = $"Quiz generation failed: {exception.Message}";
            IsEditorVisible = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StartCustomQuiz()
    {
        var blocks = QuizEntries
            .Select(entry => new GameBlock { Term = entry.Term.Trim(), Definition = entry.Definition.Trim() })
            .Where(block => !string.IsNullOrWhiteSpace(block.Term) && !string.IsNullOrWhiteSpace(block.Definition))
            .ToArray();

        if (blocks.Length < 2)
        {
            StatusMessage = "Add at least two complete question and answer pairs.";
            IsEditorVisible = true;
            return;
        }

        quiz = new Quiz
        {
            Title = string.IsNullOrWhiteSpace(CustomQuizTitle) ? "Custom quiz" : CustomQuizTitle.Trim(),
            Blocks = blocks
        };
        Title = quiz.Title;
        StartNewGame();
        IsEditorVisible = false;
        StatusMessage = $"Custom quiz loaded with {blocks.Length} pairs. Drag to connect each match.";
    }

    [RelayCommand]
    private void StartNewGame()
    {
        engine.StartNewGame(quiz.Blocks);
        Score = 0;
        MatchedPairs = 0;
        Combo = 0;
        MatchFeedback = string.Empty;
        SelectedTileIndex = 0;
        StatusMessage = "Drag each separate question or answer tile anywhere it fits.";
        RefreshTiles();
    }

    [RelayCommand]
    private void SelectTile(string? indexText)
    {
        if (!int.TryParse(indexText, out var index) || index < 0 || index >= Tiles.Count)
        {
            return;
        }

        SelectedTileIndex = index;
        var tile = Tiles[index];
        StatusMessage = tile.IsQuestion
            ? "Question tile selected. Place it anywhere it fits, then connect its answer."
            : "Answer tile selected. Place it next to its matching question.";
        BoardVersion++;
    }

    [RelayCommand]
    private void PlaceCell(string? cell)
    {
        if (SelectedTileIndex < 0 || SelectedTileIndex >= Tiles.Count || string.IsNullOrWhiteSpace(cell))
        {
            return;
        }

        var parts = cell.Split(',');
        if (parts.Length == 2 && int.TryParse(parts[0], out var row) && int.TryParse(parts[1], out var column))
        {
            TryPlaceTile(Tiles[SelectedTileIndex], row, column);
        }
    }

    public bool TryPlaceTile(PuzzleTile tile, int row, int column)
    {
        if (!Tiles.Contains(tile))
        {
            return false;
        }

        var result = engine.Place(tile, row, column);
        if (!result.Success)
        {
            StatusMessage = "That tile does not fit there or would make the remaining puzzle unsolvable.";
            BoardVersion++;
            return false;
        }

        Score = result.Score;
        if (result.PairConnected)
        {
            MatchedPairs = engine.ConnectedPairIds.Count;
            Combo++;
            MatchFeedback = Combo > 1 ? $"MATCHED • {Combo}× COMBO" : "MATCHED • PAIR CONNECTED";
            StatusMessage = "Correct pair connection. The link is locked into the board.";
        }
        else if (result.TouchesWrongPair)
        {
            Combo = 0;
            MatchFeedback = "NEAR MISS • WRONG NEIGHBOR";
            StatusMessage = "The tile fits, but it is touching a different pair. Try connecting its matching tile nearby.";
        }
        else
        {
            Combo = 0;
            MatchFeedback = "TILE PLACED";
            StatusMessage = "Tile placed. Now connect its matching question or answer tile.";
        }

        RefreshTiles();
        SelectedTileIndex = Tiles.Count > 0 ? 0 : -1;
        if (engine.IsComplete)
        {
            StatusMessage = $"Complete! You matched all pairs and filled the 8×8 grid. Score: {Score}.";
        }

        BoardVersion++;
        return true;
    }

    private void RefreshTiles()
    {
        Tiles.Clear();
        foreach (var tile in engine.RemainingTiles)
        {
            Tiles.Add(tile);
        }

        BoardVersion++;
    }
}
