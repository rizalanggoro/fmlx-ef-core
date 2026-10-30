using System.Text;
using Spectre.Console;
using TodoList.Core.Services;

namespace TodoList.Cli.UI;

public class ConsoleApp
{
    private readonly CategoryView _categoryView;
    private readonly ITodoService _todoService;
    private readonly TodoView _todoView;

    public ConsoleApp(ITodoService todoService, ICategoryService categoryService)
    {
        _todoService = todoService;
        _categoryView = new CategoryView(categoryService);
        _todoView = new TodoView(todoService, _categoryView, categoryService);
    }

    public async Task RunAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;

        while (true)
        {
            AnsiConsole.Clear();
            RenderHeader();

            var todos = await _todoService.GetAllTodosAsync();
            _todoView.RenderTodoList(todos, "Main Task List");

            AnsiConsole.WriteLine();

            var menu = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Navigation Menu:[/]")
                    .PageSize(10)
                    .AddChoices(
                        "Add Todo",
                        "Toggle Status (Done/Pending)",
                        "Edit Todo",
                        "Delete Todo",
                        "Filter Todos",
                        "Manage Categories",
                        "Refresh View",
                        "Exit"
                    ));

            switch (menu)
            {
                case "Add Todo":
                    await _todoView.AddTodoAsync();
                    break;

                case "Toggle Status (Done/Pending)":
                    await _todoView.ToggleStatusAsync();
                    break;

                case "Edit Todo":
                    await _todoView.EditTodoAsync();
                    break;

                case "Delete Todo":
                    await _todoView.DeleteTodoAsync();
                    break;

                case "Filter Todos":
                    await _todoView.FilterViewAsync();
                    break;

                case "Manage Categories":
                    await _categoryView.ManageCategoriesAsync();
                    break;

                case "Refresh View":
                    break;

                case "Exit":
                    AnsiConsole.MarkupLine("[bold green]Goodbye! Have a productive day.[/]");
                    return;
            }
        }
    }

    private static void RenderHeader()
    {
        AnsiConsole.Write(
            new FigletText("TODO LIST")
                .Centered()
                .Color(Color.Cyan1));

        AnsiConsole.Write(new Rule("[grey]Simple & Elegant CLI Task Manager[/]").Centered());
        AnsiConsole.WriteLine();
    }
}