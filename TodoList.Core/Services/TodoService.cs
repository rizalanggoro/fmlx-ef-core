using Microsoft.EntityFrameworkCore;
using TodoList.Core.Data;
using TodoList.Core.DTOs;
using TodoList.Core.Models;

namespace TodoList.Core.Services;

public class TodoService(TodoDbContext context) : ITodoService
{
    public async Task<List<TodoDto>> GetAllTodosAsync(int? categoryId = null, bool? isCompleted = null)
    {
        var query = context.Todos
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (isCompleted.HasValue)
            query = query.Where(t => t.IsCompleted == isCompleted.Value);

        return await query
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => new TodoDto(
                t.Id,
                t.Title,
                t.Description,
                t.IsCompleted,
                t.CreatedAt,
                t.DueDate,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null
            ))
            .ToListAsync();
    }

    public async Task<TodoDto?> GetTodoByIdAsync(int id)
    {
        return await context.Todos
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TodoDto(
                t.Id,
                t.Title,
                t.Description,
                t.IsCompleted,
                t.CreatedAt,
                t.DueDate,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<TodoDto> CreateTodoAsync(CreateTodoDto dto)
    {
        var trimmedTitle = dto.Title.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTitle))
            throw new ArgumentException("Todo title cannot be empty.", nameof(dto.Title));

        string? categoryName = null;
        if (dto.CategoryId.HasValue)
        {
            var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value);
            if (category == null)
                throw new ArgumentException($"Category with ID {dto.CategoryId.Value} was not found.",
                    nameof(dto.CategoryId));

            categoryName = category.Name;
        }

        var todo = new Todo
        {
            Title = trimmedTitle,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CategoryId = dto.CategoryId,
            DueDate = dto.DueDate,
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        return new TodoDto(
            todo.Id,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.CreatedAt,
            todo.DueDate,
            todo.CategoryId,
            categoryName
        );
    }

    public async Task<TodoDto?> UpdateTodoAsync(int id, UpdateTodoDto dto)
    {
        var todo = await context.Todos.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
        if (todo == null)
            return null;

        var trimmedTitle = dto.Title.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTitle))
            throw new ArgumentException("Todo title cannot be empty.", nameof(dto.Title));

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await context.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value);
            if (!categoryExists)
                throw new ArgumentException($"Category with ID {dto.CategoryId.Value} was not found.",
                    nameof(dto.CategoryId));
        }

        todo.Title = trimmedTitle;
        todo.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        todo.CategoryId = dto.CategoryId;
        todo.DueDate = dto.DueDate;

        await context.SaveChangesAsync();

        if (todo.CategoryId.HasValue) await context.Entry(todo).Reference(t => t.Category).LoadAsync();

        return new TodoDto(
            todo.Id,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.CreatedAt,
            todo.DueDate,
            todo.CategoryId,
            todo.Category?.Name
        );
    }

    public async Task<TodoDto?> ToggleCompletionAsync(int id)
    {
        var todo = await context.Todos.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
        if (todo == null) return null;

        todo.IsCompleted = !todo.IsCompleted;
        await context.SaveChangesAsync();

        return new TodoDto(
            todo.Id,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.CreatedAt,
            todo.DueDate,
            todo.CategoryId,
            todo.Category?.Name
        );
    }

    public async Task<bool> DeleteTodoAsync(int id)
    {
        var todo = await context.Todos.FindAsync(id);
        if (todo == null) return false;

        context.Todos.Remove(todo);
        await context.SaveChangesAsync();

        return true;
    }
}