using Microsoft.EntityFrameworkCore;
using TodoList.Core.Models;

namespace TodoList.Core.Data;

public class TodoDbContext(string dbPath) : DbContext
{
    private readonly string? _dbPath = dbPath;

    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        var dbPath = _dbPath ?? "todolist.db";
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(
            entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(c => c.Name).IsUnique();
            }
        );

        modelBuilder.Entity<Todo>(
            entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(250);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.IsCompleted).HasDefaultValue(false);
                entity.Property(t => t.CreatedAt).IsRequired();

                entity.HasOne(t => t.Category)
                    .WithMany(c => c.Todos)
                    .HasForeignKey(t => t.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            }
        );
    }
}