using Microsoft.EntityFrameworkCore;
using TodoList.Core.Data;
using TodoList.Core.DTOs;
using TodoList.Core.Models;

namespace TodoList.Core.Services;

public class CategoryService(TodoDbContext context) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        return await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Todos.Count))
            .ToListAsync();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        return await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Todos.Count))
            .FirstOrDefaultAsync();
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var trimmed = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Category name cannot be empty.", nameof(dto.Name));

        var exists = await context.Categories.AnyAsync(c => c.Name.ToLower() == trimmed.ToLower());
        if (exists)
            throw new InvalidOperationException($"Category '{trimmed}' already exists.");

        var category = new Category { Name = trimmed };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return new CategoryDto(category.Id, category.Name, 0);
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category == null)
            return false;

        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        return true;
    }
}
