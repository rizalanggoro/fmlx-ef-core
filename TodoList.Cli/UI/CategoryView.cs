using Spectre.Console;
using TodoList.Core.DTOs;
using TodoList.Core.Services;

namespace TodoList.Cli.UI;

public class CategoryView(ICategoryService categoryService)
{
    public async Task ManageCategoriesAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]Category Management[/]").LeftJustified());

            var categories = await categoryService.GetAllCategoriesAsync();

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(new TableColumn("[bold]ID[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Category Name[/]"));
            table.AddColumn(new TableColumn("[bold]Todos[/]").Centered());

            if (categories.Count == 0)
            {
                table.AddRow("-", "[grey]No categories found[/]", "-");
            }
            else
            {
                foreach (var cat in categories)
                {
                    table.AddRow(cat.Id.ToString(), $"[cyan]{Markup.Escape(cat.Name)}[/]", cat.TodoCount.ToString());
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select action:")
                    .AddChoices(
                        "Add New Category",
                        "Delete Category",
                        "Back to Main Menu"
                    ));

            if (choice == "Add New Category")
                await AddCategoryAsync();
            else if (choice == "Delete Category")
                await DeleteCategoryAsync(categories);
            else
                break;
        }
    }

    private async Task AddCategoryAsync()
    {
        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter new category name (or leave empty to cancel):")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            await categoryService.CreateCategoryAsync(new CreateCategoryDto(name));
            AnsiConsole.MarkupLine("[green](+) Category added successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red](!) Error: {Markup.Escape(ex.Message)}[/]");
        }

        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private async Task DeleteCategoryAsync(List<CategoryDto> categories)
    {
        if (categories.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No categories available to delete.[/]");
            Thread.Sleep(1000);
            return;
        }

        var prompt = new SelectionPrompt<CategoryDto>()
            .Title("Select a category to delete:")
            .UseConverter(c => c.Id == 0 ? "Cancel" : $"#{c.Id} {Markup.Escape(c.Name)} ({c.TodoCount} todos)");

        prompt.AddChoice(new CategoryDto(0, "Cancel"));
        foreach (var cat in categories) prompt.AddChoice(cat);

        var selected = AnsiConsole.Prompt(prompt);
        if (selected.Id == 0) return;

        var confirm = await AnsiConsole.ConfirmAsync(
            $"Are you sure you want to delete category '[yellow]{Markup.Escape(selected.Name)}[/]'? Associated todos will be set to uncategorized.");

        if (confirm)
        {
            var success = await categoryService.DeleteCategoryAsync(selected.Id);
            AnsiConsole.MarkupLine(success
                ? "[green](+) Category deleted successfully![/]"
                : "[red](!) Category not found.[/]");
            Thread.Sleep(1200);
        }
    }

    public async Task<int?> SelectCategoryOptionalAsync(string promptTitle = "Select Category (optional):")
    {
        var categories = await categoryService.GetAllCategoriesAsync();
        if (categories.Count == 0) return null;

        var prompt = new SelectionPrompt<CategoryDto>()
            .Title(promptTitle)
            .UseConverter(c => c.Id == 0 ? "[grey]-- None --[/]" : Markup.Escape(c.Name));

        prompt.AddChoice(new CategoryDto(0, "None"));
        foreach (var cat in categories) prompt.AddChoice(cat);

        var selected = AnsiConsole.Prompt(prompt);
        return selected.Id == 0 ? null : selected.Id;
    }
}
