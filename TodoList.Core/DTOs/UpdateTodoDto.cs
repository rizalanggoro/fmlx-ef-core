namespace TodoList.Core.DTOs;

public record UpdateTodoDto(
    string Title,
    string? Description = null,
    int? CategoryId = null,
    DateTime? DueDate = null
);
