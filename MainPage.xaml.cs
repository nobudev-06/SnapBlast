using SnapBlast.ViewModels;

namespace SnapBlast;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        var playView = this.FindByName<Views.PuzzlePlayView>("PlayView");
        playView.TileDropped += OnTileDropped;
        playView.TileDropRejected += OnTileDropRejected;
        UpdateBoard();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.BoardVersion) or nameof(MainViewModel.SelectedTileIndex))
        {
            UpdateBoard();
        }
    }

    private void UpdateBoard()
    {
        var playView = this.FindByName<Views.PuzzlePlayView>("PlayView");
        playView.Engine = viewModel.Engine;
        playView.SelectedTileIndex = viewModel.SelectedTileIndex;
        playView.Refresh();
    }

    private void OnTileDropped(object? sender, Views.TileDropEventArgs e)
    {
        viewModel.TryPlaceTile(e.Tile, e.Row, e.Column);
    }

    private void OnTileDropRejected(object? sender, EventArgs e)
    {
        viewModel.StatusMessage = "That tile does not perfectly fit there, so it was returned to the tray.";
    }
}
