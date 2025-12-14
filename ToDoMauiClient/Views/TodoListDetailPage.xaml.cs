using ToDoMauiClient.ViewModels;

namespace ToDoMauiClient.Views;

public partial class TodoListDetailPage : ContentPage
{
	public TodoListDetailPage(TodoListDetailViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TodoListDetailViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}   