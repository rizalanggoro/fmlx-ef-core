namespace TodoList.Core.DTOs;

public record CreateTodoDto(
    string Title,
    string? Description = null,
    int? CategoryId = null,
    DateTime? DueDate = null
);
