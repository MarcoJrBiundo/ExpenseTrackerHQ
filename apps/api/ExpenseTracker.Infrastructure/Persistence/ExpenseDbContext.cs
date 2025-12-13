using System;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence;

public sealed class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options) : DbContext(options)
{
    public DbSet<Expense> Expenses { get; set; } = default!;

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }   


    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var utcNow = DateTime.UtcNow;

    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.SetCreated(utcNow);
            entry.Entity.SetUpdated(utcNow);
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.SetUpdated(utcNow);
        }
    }

    return base.SaveChangesAsync(cancellationToken);
}
}
