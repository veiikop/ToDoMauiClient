using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls; // ← Обязательно для [QueryProperty]
using System.Collections.ObjectModel;
using ToDoMauiClient.DTO;
using ToDoMauiClient.Services;

namespace ToDoMauiClient.ViewModels;

/// <summary>
/// ViewModel для детальной страницы одного списка задач
/// Автоматически получает listId из параметра навигации благодаря [QueryProperty]
/// </summary>
[QueryProperty(nameof(ListId), "listId")] 
public partial class TodoListDetailViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private int listId;

    [ObservableProperty]
    private string listTitle = "Список задач";

    [ObservableProperty]
    private ObservableCollection<TodoItemDTO> tasks = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isModalVisible;

    [ObservableProperty]
    private TodoItemDTO editingTask = new();

    [ObservableProperty]
    private string modalTitle = "Новая задача";

    [ObservableProperty]
    private bool isNewTask = true;

    public DateTime Today => DateTime.Today;

    public List<Priority> PriorityItems => Enum.GetValues(typeof(Priority)).Cast<Priority>().ToList();

    public TodoListDetailViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    // Этот метод вызывается автоматически, когда Shell устанавливает ListId
    // Здесь мы можем сразу начать загрузку задач
    partial void OnListIdChanged(int value)
    {
        listId = value;
        // Можно сразу загрузить задачи, но лучше в OnAppearing страницы,
        // чтобы избежать лишних вызовов при навигации назад
    }

    /// <summary>
    /// Вызывается из TodoListDetailPage.xaml.cs в OnAppearing()
    /// </summary>
    public async Task InitializeAsync()
    {
        if (ListId <= 0)
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Некорректный ID списка", "OK");
            return;
        }

        ListTitle = $"Список задач #{ListId}";

        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _apiService.GetTodoItemsByListIdAsync(ListId);

            Tasks.Clear();
            foreach (var item in items)
            {
                Tasks.Add(item);
            }
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось загрузить задачи", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadTasksAsync();
    }

    [RelayCommand]
    private void AddTask()
    {
        IsNewTask = true;
        ModalTitle = "Новая задача";
        EditingTask = new TodoItemDTO
        {
            TodoListId = ListId,
            Priority = Priority.Medium
        };
        IsModalVisible = true;
    }

    [RelayCommand]
    private void EditTask(TodoItemDTO task)
    {
        if (task == null) return;

        IsNewTask = false;
        ModalTitle = "Редактировать задачу";
        EditingTask = new TodoItemDTO
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Priority = task.Priority,
            IsCompleted = task.IsCompleted,
            TodoListId = ListId
        };
        IsModalVisible = true;
    }

    [RelayCommand]
    private async Task SaveTask()
    {
        if (string.IsNullOrWhiteSpace(EditingTask.Title))
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Введите название задачи", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            if (IsNewTask)
            {
                var created = await _apiService.CreateTodoItemAsync(new CreateTodoItemDTO
                {
                    Title = EditingTask.Title,
                    Description = EditingTask.Description,
                    DueDate = EditingTask.DueDate,
                    Priority = EditingTask.Priority,
                    TodoListId = ListId
                });

                Tasks.Add(created);
            }
            else
            {
                var updated = await _apiService.UpdateTodoItemAsync(EditingTask.Id, new UpdateTodoItemDTO
                {
                    Title = EditingTask.Title,
                    Description = EditingTask.Description,
                    DueDate = EditingTask.DueDate,
                    Priority = EditingTask.Priority,
                    IsCompleted = EditingTask.IsCompleted
                });

                if (updated != null)
                {
                    var existing = Tasks.FirstOrDefault(t => t.Id == updated.Id);
                    if (existing != null)
                    {
                        var index = Tasks.IndexOf(existing);
                        Tasks[index] = updated;
                    }
                }
            }

            CloseModal();
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось сохранить задачу", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleComplete(TodoItemDTO task)
    {
        if (task == null) return;

        IsBusy = true;
        try
        {
            var updated = await _apiService.UpdateTodoItemAsync(task.Id, new UpdateTodoItemDTO
            {
                IsCompleted = !task.IsCompleted
            });

            if (updated != null)
            {
                var existing = Tasks.FirstOrDefault(t => t.Id == task.Id);
                if (existing != null)
                {
                    var index = Tasks.IndexOf(existing);
                    Tasks[index] = updated;
                }
            }
        }
        catch
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось обновить статус", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteTask(TodoItemDTO task)
    {
        if (task == null) return;

        bool confirm = await Application.Current?.MainPage?.DisplayAlert(
            "Удалить?", $"Удалить задачу \"{task.Title}\"?", "Да", "Нет");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            await _apiService.DeleteTodoItemAsync(task.Id);
            Tasks.Remove(task);
        }
        catch
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось удалить задачу", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CloseModal()
    {
        IsModalVisible = false;
        EditingTask = null;
    }
}