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

    [ObservableProperty]
    private TodoListDTO currentList = new();

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

        IsBusy = true;
        try
        {
            var todoList = await _apiService.GetTodoListByIdAsync(ListId);
            if (todoList != null)
            {
                ListTitle = todoList.Title ?? "Список задач"; 
            }
            else
            {
                ListTitle = "Список задач (ошибка загрузки)";
            }
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", $"Не удалось загрузить список: {ex.Message}", "OK");
            ListTitle = "Список задач (ошибка)";
        }
        finally
        {
            IsBusy = false;
        }
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
            Priority = Priority.Medium,
            Title = string.Empty,         // явно пусто
            Description = string.Empty,
            DueDate = null,
            IsCompleted = false
        };
        IsModalVisible = true;
    }

    [RelayCommand]
    private void EditTask(TodoItemDTO task)
    {
        if (task == null) return;

        IsNewTask = false;
        ModalTitle = "Редактировать задачу";

        // Важно: создаём НОВЫЙ объект и копируем ВСЕ поля
        EditingTask = new TodoItemDTO
        {
            Id = task.Id,
            Title = task.Title ?? string.Empty,           // на всякий случай защита от null
            Description = task.Description ?? string.Empty,
            DueDate = task.DueDate,
            Priority = task.Priority,
            IsCompleted = task.IsCompleted,
            TodoListId = ListId,
            CreatedAt = task.CreatedAt,
            CompletedAt = task.CompletedAt
        };

        IsModalVisible = true;
    }

    [RelayCommand]
    private async Task SaveTask()
    {
        // Строгая проверка ДО всего
        if (EditingTask == null || string.IsNullOrWhiteSpace(EditingTask.Title))
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Введите название задачи!", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var dueDateUtc = EditingTask.DueDate.HasValue
                ? DateTime.SpecifyKind(EditingTask.DueDate.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            if (IsNewTask)
            {
                var created = await _apiService.CreateTodoItemAsync(new CreateTodoItemDTO
                {
                    Title = EditingTask.Title.Trim(),
                    Description = EditingTask.Description?.Trim(),
                    DueDate = dueDateUtc,
                    Priority = EditingTask.Priority,
                    TodoListId = ListId
                });

                Tasks.Add(created);
            }
            else
            {
                // Для обновления отправляем Title всегда — он проверен выше
                var updateDto = new UpdateTodoItemDTO
                {
                    Title = EditingTask.Title.Trim(),
                    Description = EditingTask.Description?.Trim(),
                    DueDate = dueDateUtc,
                    Priority = EditingTask.Priority,
                    IsCompleted = EditingTask.IsCompleted
                };

                var updated = await _apiService.UpdateTodoItemAsync(EditingTask.Id, updateDto);

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
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", $"Не удалось сохранить: {ex.Message}", "OK");
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
            // отправляем title чтоб сервер не пытался обнулить его
            var updated = await _apiService.UpdateTodoItemAsync(task.Id, new UpdateTodoItemDTO
            {
                IsCompleted = !task.IsCompleted,
                Title = task.Title,
                Description = task.Description
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
        catch (Exception ex)
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

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("///todolists");
    }
}