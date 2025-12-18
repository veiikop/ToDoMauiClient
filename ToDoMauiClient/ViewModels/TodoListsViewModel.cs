using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ToDoMauiClient.DTO;
using ToDoMauiClient.Services;

namespace ToDoMauiClient.ViewModels;

public partial class TodoListsViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<TodoListDTO> todoLists = new();

    [ObservableProperty]
    private bool isBusy;

    public TodoListsViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadListsAsync()
    {
        IsBusy = true;
        try
        {
            var lists = await _apiService.GetTodoListsAsync();
            TodoLists.Clear();
            foreach (var list in lists)
            {
                TodoLists.Add(list);
            }
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось загрузить списки задач", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadListsAsync();
    }

    [RelayCommand]
    private async Task AddList()
    {
        string title = await Application.Current?.MainPage?.DisplayPromptAsync(
            "Новый список", "Введите название списка", "Создать", "Отмена", "Мой новый список");

        if (string.IsNullOrWhiteSpace(title))
            return;

        IsBusy = true;
        try
        {
            var newList = await _apiService.CreateTodoListAsync(new CreateTodoListDTO
            {
                Title = title,
                Description = null
            });

            TodoLists.Add(newList);
        }
        catch
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось создать список", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteList(TodoListDTO list)
    {
        if (list == null) return;

        bool confirm = await Application.Current?.MainPage?.DisplayAlert(
            "Удалить список?", $"Вы уверены, что хотите удалить список \"{list.Title}\" и все его задачи?", "Да", "Нет");

        if (confirm)
        {
            IsBusy = true;
            try
            {
                await _apiService.DeleteTodoListAsync(list.Id);
                TodoLists.Remove(list);
            }
            catch
            {
                await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось удалить список", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task SelectList(TodoListDTO list)
    {
        if (list == null) return;
        await Shell.Current.GoToAsync($"//todolistdetail?listId={list.Id}");
    }

    [RelayCommand]
    private async Task Logout()
    {
        // Очищаем токены
        if (Application.Current?.Windows[0]?.Handler?.MauiContext?.Services.GetService<IApiService>() is ApiService apiService)
        {
            apiService.ClearTokens();
        }
        await Shell.Current.GoToAsync("///login");
    }

    [RelayCommand]
    private async Task GoToProfile()
    {
        await Shell.Current.GoToAsync("///profile");
    }
}