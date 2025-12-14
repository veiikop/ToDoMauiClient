// Services/ApiService.cs
using System.Text;
using System.Text.Json;
using ToDoMauiClient.DTO;

namespace ToDoMauiClient.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://10.0.2.2:7072/api"; // ← ИЗМЕНИ НА СВОЙ АДРЕС И ПОРТ!

    private string? _accessToken;
    private string? _refreshToken;

    public ApiService()
    {
        var handler = new HttpClientHandler();

#if DEBUG
        // Игнорируем самоподписанный сертификат для локальной разработки
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // При запуске пытаемся загрузить сохранённые токены
        LoadTokensFromStorage();
    }

    /// <summary>
    /// Загружает токены из Preferences и устанавливает заголовок Authorization
    /// </summary>
    private void LoadTokensFromStorage()
    {
        _accessToken = Preferences.Get("AccessToken", null);
        _refreshToken = Preferences.Get("RefreshToken", null);

        if (!string.IsNullOrEmpty(_accessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    /// <summary>
    /// Сохраняет токены и устанавливает Bearer-заголовок
    /// </summary>
    private void SetTokens(string accessToken, string refreshToken)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;

        Preferences.Set("AccessToken", accessToken);
        Preferences.Set("RefreshToken", refreshToken);

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    /// <summary>
    /// Очищает токены (для выхода из аккаунта)
    /// </summary>
    public void ClearTokens()
    {
        _accessToken = null;
        _refreshToken = null;

        Preferences.Remove("AccessToken");
        Preferences.Remove("RefreshToken");

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Универсальный метод отправки запроса с обработкой 401 и refresh
    /// </summary>
    private async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object? data = null)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{endpoint}");

        if (data != null)
        {
            var json = JsonSerializer.Serialize(data);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);

        // Если 401 — пробуем обновить токен
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_refreshToken))
        {
            var refreshSuccess = await RefreshTokenAsync();
            if (refreshSuccess.Success)
            {
                // Повторяем исходный запрос с новым токеном
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                response = await _httpClient.SendAsync(request);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Ошибка {response.StatusCode}: {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            return default(T)!;

        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    // ==================== АВТОРИЗАЦИЯ ====================

    public async Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/Auth/register",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponseDTO>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        if (result.Success)
            SetTokens(result.Token, result.RefreshToken);

        return result;
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/Auth/login",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponseDTO>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        if (result.Success)
            SetTokens(result.Token, result.RefreshToken);

        return result;
    }

    public async Task<AuthResponseDTO> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_refreshToken))
            return new AuthResponseDTO { Success = false, ErrorMessage = "Нет токенов для обновления" };

        var request = new RefreshTokenRequestDTO
        {
            Token = _accessToken,
            RefreshToken = _refreshToken
        };

        var response = await _httpClient.PostAsync($"{_baseUrl}/Auth/refresh",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResponseDTO>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        if (result.Success)
            SetTokens(result.Token, result.RefreshToken);

        return result;
    }

    // ==================== СПИСКИ ЗАДАЧ ====================

    public async Task<List<TodoListDTO>> GetTodoListsAsync()
        => await SendRequestAsync<List<TodoListDTO>>(HttpMethod.Get, "/TodoLists");

    public async Task<TodoListDTO> CreateTodoListAsync(CreateTodoListDTO dto)
        => await SendRequestAsync<TodoListDTO>(HttpMethod.Post, "/TodoLists", dto);

    public async Task<TodoListDTO?> UpdateTodoListAsync(int id, CreateTodoListDTO dto)
        => await SendRequestAsync<TodoListDTO?>(HttpMethod.Put, $"/TodoLists/{id}", dto);

    public async Task<bool> DeleteTodoListAsync(int id)
    {
        await SendRequestAsync<object>(HttpMethod.Delete, $"/TodoLists/{id}");
        return true;
    }

    // ==================== ЗАДАЧИ ====================

    public async Task<List<TodoItemDTO>> GetTodoItemsByListIdAsync(int todoListId)
        => await SendRequestAsync<List<TodoItemDTO>>(HttpMethod.Get, $"/TodoItems/by-list/{todoListId}");

    public async Task<TodoItemDTO> CreateTodoItemAsync(CreateTodoItemDTO dto)
        => await SendRequestAsync<TodoItemDTO>(HttpMethod.Post, "/TodoItems", dto);

    public async Task<TodoItemDTO?> UpdateTodoItemAsync(int id, UpdateTodoItemDTO dto)
        => await SendRequestAsync<TodoItemDTO?>(HttpMethod.Put, $"/TodoItems/{id}", dto);

    public async Task<bool> DeleteTodoItemAsync(int id)
    {
        await SendRequestAsync<object>(HttpMethod.Delete, $"/TodoItems/{id}");
        return true;
    }

    // ==================== ПРОФИЛЬ ====================

    public async Task<UserDTO> GetCurrentUserProfileAsync()
        => await SendRequestAsync<UserDTO>(HttpMethod.Get, "/Users/profile");
}