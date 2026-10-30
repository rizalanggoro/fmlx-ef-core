using TodoList.Core.Data;
using TodoList.Core.DTOs;
using TodoList.Core.Services;

namespace TodoList.Tests;

public class CoreSanityTests
{
    private TodoDbContext _context = null!;
    private CategoryService _categoryService = null!;
    private TodoService _todoService = null!;
    private string _dbFile = null!;

    [SetUp]
    public async Task Setup()
    {
        _dbFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
        _context = new TodoDbContext(_dbFile);
        await _context.Database.EnsureCreatedAsync();

        _categoryService = new CategoryService(_context);
        _todoService = new TodoService(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
        if (File.Exists(_dbFile))
        {
            try { File.Delete(_dbFile); } catch { }
        }
    }

    [Test]
    public async Task Can_Create_Category_And_Todo_With_Dto()
    {
        var category = await _categoryService.CreateCategoryAsync(new CreateCategoryDto("Work"));
        Assert.That(category.Id, Is.GreaterThan(0));

        var todo = await _todoService.CreateTodoAsync(new CreateTodoDto(
            Title: "Finish Project Report",
            Description: "Team monthly report",
            CategoryId: category.Id,
            DueDate: DateTime.UtcNow.AddDays(1)
        ));

        Assert.That(todo.Id, Is.GreaterThan(0));
        Assert.That(todo.CategoryName, Is.EqualTo("Work"));

        var allTodos = await _todoService.GetAllTodosAsync();
        Assert.That(allTodos.Count, Is.EqualTo(1));

        var toggled = await _todoService.ToggleCompletionAsync(todo.Id);
        Assert.That(toggled!.IsCompleted, Is.True);
    }
}
