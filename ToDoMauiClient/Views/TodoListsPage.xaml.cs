using ToDoMauiClient.DTO;
using ToDoMauiClient.ViewModels;

namespace ToDoMauiClient.Views;

public partial class TodoListsPage : ContentPage
{
    public TodoListsPage(TodoListsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // «агружаем списки при по€влении страницы
        Appearing += async (s, e) => await viewModel.LoadListsAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TodoListsViewModel vm)
        {
            await vm.LoadListsAsync();
        }
    }
}