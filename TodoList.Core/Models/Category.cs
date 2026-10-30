namespace TodoList.Core.Models;

public class Category
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public ICollection<Todo> Todos { get; init; } = new List<Todo>();
}