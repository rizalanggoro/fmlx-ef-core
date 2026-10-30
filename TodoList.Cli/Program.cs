using System.Text;
using Spectre.Console;
using TodoList.Cli.UI;
using TodoList.Core.Data;
using TodoList.Core.Models;
using TodoList.Core.Services;

Console.OutputEncoding = Encoding.UTF8;

var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "todolist.db");
await using var db = new TodoDbContext(dbPath);

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Initializing SQLite database...", async _ =>
    {
        await db.Database.EnsureCreatedAsync();

        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "Work" },
                new Category { Name = "Personal" },
                new Category { Name = "Study" }
            );

            await db.SaveChangesAsync();
        }
    });

var categoryService = new CategoryService(db);
var todoService = new TodoService(db);

var app = new ConsoleApp(todoService, categoryService);
await app.RunAsync();