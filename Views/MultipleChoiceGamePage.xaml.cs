using SnapBlast.ViewModels;

namespace SnapBlast.Views;

public partial class MultipleChoiceGamePage : ContentPage
{
    public MultipleChoiceGamePage(MultipleChoiceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}