namespace TodoList.Core.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    int TodoCount = 0
);