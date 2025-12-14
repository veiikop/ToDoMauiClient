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

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is TodoListDTO list)
        {
            ((CollectionView)sender).SelectedItem = null;

            var route = $"//todolistdetail?listId={list.Id}";
            await Shell.Current.GoToAsync(route);
        }
    }
}