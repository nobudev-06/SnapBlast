using SnapBlast.ViewModels;

namespace SnapBlast.Views;

public partial class GameModeSelectionPage : ContentPage
{
    private readonly GameModeSelectionViewModel viewModel;

    public GameModeSelectionPage(GameModeSelectionViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Shell keeps this page alive, so refresh the greeting every time it's shown.
        viewModel.Refresh();
    }
}
