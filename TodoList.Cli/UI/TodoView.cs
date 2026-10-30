using System.Globalization;
using Spectre.Console;
using TodoList.Core.DTOs;
using TodoList.Core.Services;

namespace TodoList.Cli.UI;

public class TodoView(
    ITodoService todoService,
    CategoryView categoryView,
    ICategoryService categoryService
)
{
    public void RenderTodoList(List<TodoDto> todos, string headerTitle = "Tasks (Todo List)")
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();

        table.AddColumn(new TableColumn("[bold]ID[/]").Centered().Width(5));
        table.AddColumn(new TableColumn("[bold]Status[/]").Centered().Width(12));
        table.AddColumn(new TableColumn("[bold]Title[/]"));
        table.AddColumn(new TableColumn("[bold]Category[/]").Width(16));
        table.AddColumn(new TableColumn("[bold]Due Date[/]").Centered().Width(14));
        table.AddColumn(new TableColumn("[bold]Description[/]"));

        if (todos.Count == 0)
        {
            table.AddRow("-", "-", "[grey]No todos found.[/]", "-", "-", "-");
        }
        else
        {
            var now = DateTime.UtcNow.Date;
            foreach (var item in todos)
            {
                var idStr = item.Id.ToString();
                var statusStr = item.IsCompleted
                    ? "[green][[DONE]][/]"
                    : "[yellow][[TODO]][/]";

                var titleStr = item.IsCompleted
                    ? $"[strikethrough grey]{Markup.Escape(item.Title)}[/]"
                    : $"[bold white]{Markup.Escape(item.Title)}[/]";

                var categoryStr = item.CategoryName != null
                    ? $"[cyan]* {Markup.Escape(item.CategoryName)}[/]"
                    : "[grey]-[/]";

                string dueDateStr;
                if (item.DueDate.HasValue)
                {
                    var due = item.DueDate.Value.Date;
                    var dateFormatted = due.ToString("yyyy-MM-dd");

                    dueDateStr = item.IsCompleted switch
                    {
                        false when due < now => $"[red bold]{dateFormatted} (!)[/]",
                        false when due == now => $"[yellow bold]{dateFormatted} (Today)[/]",
                        _ => $"[grey]{dateFormatted}[/]"
                    };
                }
                else
                {
                    dueDateStr = "[grey]-[/]";
                }

                var descStr = string.IsNullOrWhiteSpace(item.Description)
                    ? "[grey]-[/]"
                    : Markup.Escape(item.Description);

                table.AddRow(idStr, statusStr, titleStr, categoryStr, dueDateStr, descStr);
            }
        }

        var completedCount = todos.Count(t => t.IsCompleted);
        var pendingCount = todos.Count - completedCount;

        table.Caption(
            $"[grey]Total: {todos.Count} | [green]Completed: {completedCount}[/] | [yellow]Pending: {pendingCount}[/][/]");

        var panel = new Panel(table)
            .Header($"[yellow]{Markup.Escape(headerTitle)}[/]")
            .Border(BoxBorder.Rounded);

        AnsiConsole.Write(panel);
    }

    public async Task AddTodoAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Add New Todo[/]").LeftJustified());

        var title = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [green]Title[/]:")
                .Validate(t => string.IsNullOrWhiteSpace(t)
                    ? ValidationResult.Error("[red]Title cannot be empty![/]")
                    : ValidationResult.Success()));

        var description = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter [cyan]Description (optional, press Enter to skip)[/]:")
                .AllowEmpty());

        var categoryId = await categoryView.SelectCategoryOptionalAsync("Select Category for this Todo:");

        var dueDate = PromptDueDate();

        try
        {
            var created = await todoService.CreateTodoAsync(new CreateTodoDto(
                title,
                string.IsNullOrWhiteSpace(description) ? null : description,
                categoryId,
                dueDate));

            AnsiConsole.MarkupLine($"[green](+) Todo '[bold]{Markup.Escape(created.Title)}[/]' added successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red](!) Failed to add todo: {Markup.Escape(ex.Message)}[/]");
        }

        AnsiConsole.WriteLine("Press any key to return...");
        Console.ReadKey(true);
    }

    public async Task ToggleStatusAsync()
    {
        var todos = await todoService.GetAllTodosAsync();
        if (todos.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No todos available.[/]");
            Thread.Sleep(1000);
            return;
        }

        var prompt = new SelectionPrompt<TodoDto>()
            .Title("Select a todo to toggle status (Done/Pending):")
            .PageSize(10)
            .UseConverter(t =>
            {
                if (t.Id == 0)
                    return "Cancel";

                var status = t.IsCompleted ? "[green][[DONE]][/]" : "[yellow][[TODO]][/]";
                var cat = t.CategoryName != null ? $" [cyan]({t.CategoryName})[/]" : "";

                return $"{status} #{t.Id} {Markup.Escape(t.Title)}{cat}";
            });

        prompt.AddChoice(new TodoDto(0, "Cancel", null, false, DateTime.MinValue, null, null, null));

        foreach (var t in todos)
            prompt.AddChoice(t);

        var selected = AnsiConsole.Prompt(prompt);
        if (selected.Id == 0)
            return;

        var updated = await todoService.ToggleCompletionAsync(selected.Id);
        if (updated != null)
        {
            var statusText = updated.IsCompleted ? "[green]Completed[/]" : "[yellow]Pending[/]";
            AnsiConsole.MarkupLine(
                $"[green](+) Todo '[bold]{Markup.Escape(updated.Title)}[/]' is now marked as {statusText}.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red](!) Todo not found.[/]");
        }

        Thread.Sleep(1200);
    }

    public async Task EditTodoAsync()
    {
        var todos = await todoService.GetAllTodosAsync();
        if (todos.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No todos available to edit.[/]");
            Thread.Sleep(1000);
            return;
        }

        var prompt = new SelectionPrompt<TodoDto>()
            .Title("Select a todo to edit:")
            .PageSize(10)
            .UseConverter(t => t.Id == 0 ? "Cancel" : $"#{t.Id} {Markup.Escape(t.Title)}");

        prompt.AddChoice(new TodoDto(0, "Cancel", null, false, DateTime.MinValue, null, null, null));

        foreach (var t in todos) prompt.AddChoice(t);

        var selected = AnsiConsole.Prompt(prompt);
        if (selected.Id == 0)
            return;

        AnsiConsole.Write(new Rule($"[yellow]Edit Todo #{selected.Id}[/]").LeftJustified());

        var newTitle = AnsiConsole.Prompt(
            new TextPrompt<string>("New title:").DefaultValue(selected.Title)
        );

        var newDesc = AnsiConsole.Prompt(
            new TextPrompt<string>("New description (press Enter to keep or clear):")
                .DefaultValue(selected.Description ?? "")
                .AllowEmpty()
        );

        var changeCategory = await AnsiConsole.ConfirmAsync("Change category?");
        var newCategoryId = selected.CategoryId;
        if (changeCategory) newCategoryId = await categoryView.SelectCategoryOptionalAsync("Select new Category:");

        var changeDueDate = await AnsiConsole.ConfirmAsync("Change due date?");
        var newDueDate = selected.DueDate;
        if (changeDueDate) newDueDate = PromptDueDate();

        try
        {
            await todoService.UpdateTodoAsync(
                selected.Id,
                new UpdateTodoDto(
                    newTitle,
                    string.IsNullOrWhiteSpace(newDesc) ? null : newDesc,
                    newCategoryId,
                    newDueDate));

            AnsiConsole.MarkupLine("[green](+) Todo updated successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red](!) Failed to update todo: {Markup.Escape(ex.Message)}[/]");
        }

        AnsiConsole.WriteLine("Press any key to return...");
        Console.ReadKey(true);
    }

    public async Task DeleteTodoAsync()
    {
        var todos = await todoService.GetAllTodosAsync();
        if (todos.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No todos available to delete.[/]");
            Thread.Sleep(1000);
            return;
        }

        var prompt = new SelectionPrompt<TodoDto>()
            .Title("Select a todo to delete:")
            .PageSize(10)
            .UseConverter(t => t.Id == 0 ? "Cancel" : $"#{t.Id} {Markup.Escape(t.Title)}");

        prompt.AddChoice(new TodoDto(0, "Cancel", null, false, DateTime.MinValue, null, null, null));

        foreach (var t in todos)
            prompt.AddChoice(t);

        var selected = AnsiConsole.Prompt(prompt);
        if (selected.Id == 0)
            return;

        var confirm = await AnsiConsole.ConfirmAsync(
            $"Are you sure you want to delete todo '[yellow]{Markup.Escape(selected.Title)}[/]'?"
        );

        if (confirm)
        {
            var success = await todoService.DeleteTodoAsync(selected.Id);
            AnsiConsole.MarkupLine(success
                ? "[green](+) Todo deleted successfully![/]"
                : "[red](!) Todo not found.[/]");
            Thread.Sleep(1200);
        }
    }

    public async Task FilterViewAsync()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select filter option:")
                .AddChoices(
                    "Filter by Category",
                    "Show Pending Only",
                    "Show Completed Only",
                    "Back"
                )
        );

        switch (choice)
        {
            case "Filter by Category":
            {
                var categories = await categoryService.GetAllCategoriesAsync();
                if (categories.Count == 0)
                {
                    AnsiConsole.MarkupLine("[grey]No categories registered.[/]");
                    Thread.Sleep(1000);
                    return;
                }

                var catPrompt = new SelectionPrompt<CategoryDto>()
                    .Title("Select category:")
                    .UseConverter(c => c.Name);

                foreach (var cat in categories)
                    catPrompt.AddChoice(cat);

                var selectedCat = AnsiConsole.Prompt(catPrompt);
                var filteredTodos = await todoService.GetAllTodosAsync(selectedCat.Id);

                AnsiConsole.Clear();
                RenderTodoList(filteredTodos, $"Category: {selectedCat.Name}");
                AnsiConsole.WriteLine("\nPress any key to return...");
                Console.ReadKey(true);

                break;
            }

            case "Show Pending Only":
            {
                var filteredTodos = await todoService.GetAllTodosAsync(isCompleted: false);
                AnsiConsole.Clear();
                RenderTodoList(filteredTodos, "Pending Todos");
                AnsiConsole.WriteLine("\nPress any key to return...");
                Console.ReadKey(true);

                break;
            }

            case "Show Completed Only":
            {
                var filteredTodos = await todoService.GetAllTodosAsync(isCompleted: true);
                AnsiConsole.Clear();
                RenderTodoList(filteredTodos, "Completed Todos");
                AnsiConsole.WriteLine("\nPress any key to return...");
                Console.ReadKey(true);

                break;
            }
        }
    }

    private static DateTime? PromptDueDate()
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>(
                    "Due date ([italic grey]format: YYYY-MM-DD or DD-MM-YYYY, press Enter for no due date[/]):")
                .AllowEmpty()
                .Validate(str =>
                {
                    if (string.IsNullOrWhiteSpace(str)) return ValidationResult.Success();
                    var formats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "yyyy/MM/dd", "dd/MM/yyyy" };

                    return DateTime.TryParseExact(
                        str.Trim(),
                        formats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out _
                    )
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Invalid date format. Use YYYY-MM-DD (e.g. 2026-12-31)[/]");
                }));

        if (string.IsNullOrWhiteSpace(input))
            return null;

        var validFormats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "yyyy/MM/dd", "dd/MM/yyyy" };
        if (
            DateTime.TryParseExact(
                input.Trim(),
                validFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate
            )
        ) return parsedDate;

        return null;
    }
}
