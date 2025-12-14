using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ToDoMauiClient.DTO;
using ToDoMauiClient.Services;

namespace ToDoMauiClient.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    public RegisterViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    private async Task Register()
    {
        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Заполните все поля";
            HasError = true;
            return;
        }

        if (Password.Length < 8)
        {
            ErrorMessage = "Пароль должен быть минимум 8 символов";
            HasError = true;
            return;
        }

        IsBusy = true;
        HasError = false;

        try
        {
            var request = new RegisterRequestDTO
            {
                Username = Username,
                Email = Email,
                Password = Password
            };

            var response = await _apiService.RegisterAsync(request);

            if (response.Success)
            {
                // После успешной регистрации сразу переходим на списки задач
                await Shell.Current.GoToAsync("///todolists");
            }
            else
            {
                ErrorMessage = response.ErrorMessage ?? "Ошибка регистрации";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Нет соединения с сервером";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToLogin()
    {
        await Shell.Current.GoToAsync("///login");
    }
}