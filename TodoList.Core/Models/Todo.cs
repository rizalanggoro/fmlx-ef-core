namespace TodoList.Core.Models;

public class Todo
{
    public int Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; init; }
}