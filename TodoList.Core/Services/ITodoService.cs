using TodoList.Core.DTOs;

namespace TodoList.Core.Services;

public interface ITodoService
{
    Task<List<TodoDto>> GetAllTodosAsync(int? categoryId = null, bool? isCompleted = null);
    Task<TodoDto?> GetTodoByIdAsync(int id);
    Task<TodoDto> CreateTodoAsync(CreateTodoDto dto);
    Task<TodoDto?> UpdateTodoAsync(int id, UpdateTodoDto dto);
    Task<TodoDto?> ToggleCompletionAsync(int id);
    Task<bool> DeleteTodoAsync(int id);
}
