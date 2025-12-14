using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoMauiClient.DTO;

namespace ToDoMauiClient.Services
{
    public interface IApiService
    {
        // Авторизация
        Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request);
        Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<AuthResponseDTO> RefreshTokenAsync();

        // Todo Lists
        Task<List<TodoListDTO>> GetTodoListsAsync();
        Task<TodoListDTO> CreateTodoListAsync(CreateTodoListDTO dto);
        Task<TodoListDTO?> UpdateTodoListAsync(int id, CreateTodoListDTO dto);
        Task<bool> DeleteTodoListAsync(int id);
        Task<TodoListDTO> GetTodoListByIdAsync(int id);

        // Todo Items
        Task<List<TodoItemDTO>> GetTodoItemsByListIdAsync(int todoListId);
        Task<TodoItemDTO> CreateTodoItemAsync(CreateTodoItemDTO dto);
        Task<TodoItemDTO?> UpdateTodoItemAsync(int id, UpdateTodoItemDTO dto);
        Task<bool> DeleteTodoItemAsync(int id);

        // Профиль
        Task<UserDTO> GetCurrentUserProfileAsync();
        
        // очищаем токен
        void ClearTokens();
    }
}
