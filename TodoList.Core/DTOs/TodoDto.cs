namespace TodoList.Core.DTOs;

public record TodoDto(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? DueDate,
    int? CategoryId,
    string? CategoryName
);
