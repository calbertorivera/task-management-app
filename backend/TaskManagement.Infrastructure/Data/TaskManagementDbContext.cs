using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data;

public class TaskManagementDbContext : DbContext
{
    public TaskManagementDbContext(
        DbContextOptions<TaskManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");

            entity.HasKey(task => task.Id);

            entity.Property(task => task.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(task => task.Description)
                .HasMaxLength(2000);

            entity.Property(task => task.Status)
                .HasConversion<string>()
                .IsRequired();

            entity.HasOne(task => task.User)
                .WithMany(user => user.Tasks)
                .HasForeignKey(task => task.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}