using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ToDoMauiClient.DTO;
using ToDoMauiClient.Services;

namespace ToDoMauiClient.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private UserDTO user = new();

    [ObservableProperty]
    private bool isBusy;

    public ProfileViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadProfileAsync()
    {
        IsBusy = true;
        try
        {
            User = await _apiService.GetCurrentUserProfileAsync();
        }
        catch
        {
            await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось загрузить профиль", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadProfileAsync();
    }

    [RelayCommand]
    private async Task Logout()
    {
        _apiService.ClearTokens();
        await Shell.Current.GoToAsync("///login");
    }
}