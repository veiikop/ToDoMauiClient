using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ToDoMauiClient.DTO;
using ToDoMauiClient.Services;
using System.Diagnostics;

namespace ToDoMauiClient.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string emailOrUsername = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    public LoginViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(EmailOrUsername) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Заполните все поля";
            HasError = true;
            return;
        }

        IsBusy = true;
        HasError = false;

        try
        {
            var request = new LoginRequestDTO
            {
                EmailOrUsername = EmailOrUsername,
                Password = Password
            };

            var response = await _apiService.LoginAsync(request);

            // После успешного логина
            if (response.Success)
            {
                // Сохраняем имя пользователя
                Preferences.Set("Username", response.User.Username);

                // Обновляем информацию в AppShell
                if (Application.Current.MainPage is AppShell shell)
                {
                    shell.UpdateUserProfile(response.User.Username);
                }

                await Shell.Current.GoToAsync("///todolists");
            }
            else
            {
                ErrorMessage = response.ErrorMessage ?? "Ошибка входа";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            ErrorMessage = "Нет соединения с сервером";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        await Shell.Current.GoToAsync("///register");
    }
}