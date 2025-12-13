using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseTracker.Infrastructure.Tests.Builders;
using ExpenseTracker.Infrastructure.Tests.Db;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Tests.Repositories;

public class ExpensesRepositoryTests
{
    [Fact]
    public async Task GetExpensesByUserAsync_returns_only_expenses_for_that_user()
    {
        // Arrange
        using var context = SqliteExpensesDbContextFactory.CreateContext();
        var repository = new ExpensesRepository(context);

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var e1 = new ExpenseBuilder()
            .WithUserId(userId)
            .WithAmount(10m)
            .WithCategory("Food")
            .Build();

        var e2 = new ExpenseBuilder()
            .WithUserId(userId)
            .WithAmount(20m)
            .WithCategory("Transport")
            .Build();

        var e3 = new ExpenseBuilder()
            .WithUserId(otherUserId)
            .WithAmount(30m)
            .WithCategory("Other")
            .Build();

        await context.Expenses.AddRangeAsync(e1, e2, e3);
        await context.SaveChangesAsync();

        // Make sure AsNoTracking works as expected
        context.ChangeTracker.Clear();

        // Act
        var result = await repository.GetExpensesByUserAsync(userId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.UserId == userId);

        var ids = result.Select(e => e.Id).ToArray();
        ids.Should().BeEquivalentTo(new[] { e1.Id, e2.Id });

        // AsNoTracking → DbContext shouldn't be tracking returned entities
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpenseByIdAsync_returns_expense_when_it_exists_for_user()
    {
        // Arrange
        using var context = SqliteExpensesDbContextFactory.CreateContext();
        var repository = new ExpensesRepository(context);

        var userId = Guid.NewGuid();

        var expense = new ExpenseBuilder()
            .WithUserId(userId)
            .WithAmount(99.99m)
            .WithCategory("Bills")
            .WithDescription("Hydro bill")
            .Build();

        await context.Expenses.AddAsync(expense);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetExpenseByIdAsync(userId, expense.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expense.Id);
        result.UserId.Should().Be(userId);
        result.Amount.Should().Be(99.99m);
        result.Category.Should().Be("Bills");
        result.Description.Should().Be("Hydro bill");
    }

    [Fact]
    public async Task GetExpenseByIdAsync_returns_null_when_expense_does_not_exist()
    {
        // Arrange
        using var context = SqliteExpensesDbContextFactory.CreateContext();
        var repository = new ExpensesRepository(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await repository.GetExpenseByIdAsync(userId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExpenseByIdAsync_returns_null_when_expense_belongs_to_different_user()
    {
        // Arrange
        using var context = SqliteExpensesDbContextFactory.CreateContext();
        var repository = new ExpensesRepository(context);

        var correctUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var expense = new ExpenseBuilder()
            .WithUserId(otherUserId)
            .Build();

        await context.Expenses.AddAsync(expense);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetExpenseByIdAsync(correctUserId, expense.Id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_assigns_new_id_when_empty_and_tracks_entity()
    {
        // Arrange
        using var context = SqliteExpensesDbContextFactory.CreateContext();
        var repository = new ExpensesRepository(context);

        var expense = new ExpenseBuilder()
            .WithId(Guid.Empty)
            .WithAmount(50m)
            .WithCategory("Groceries")
            .Build();

        // Act
        var returnedId = await repository.AddExpenseAsync(expense, CancellationToken.None);

        // Assert: ID is set
        returnedId.Should().NotBe(Guid.Empty);
        expense.Id.Should().Be(returnedId);

        // Repo should not call SaveChangesAsync (Unit of Work handles it)
        context.ChangeTracker.Entries<Expense>()
            .Single().State.Should().Be(EntityState.Added);

        // Once SaveChangesAsync is called, it exists in DB
        await context.SaveChangesAsync();
        var fromDb = await context.Expenses.FindAsync(returnedId);
        fromDb.Should().NotBeNull();
    }

    [Fact]
    public async Task AddExpenseAsync_throws_when_expense_is_null()
    {
        // Arrange
        using var context = SqliteExpensesDbContextFactory.CreateContext();
        var repository = new ExpensesRepository(context);

        // Act
        var act = async () => await repository.AddExpenseAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteExpense_marks_entity_for_deletion_and_removes_from_database_after_save()
    {
        // Arrange
        using var context = SqliteExpensesDbContextFactory.CreateContext();
        var repository = new ExpensesRepository(context);

        var expense = new ExpenseBuilder()
            .WithCategory("Fun")
            .WithDescription("Movie tickets")
            .Build();

        await context.Expenses.AddAsync(expense);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteExpense(expense, CancellationToken.None);

        // Assert: state is Deleted before SaveChanges
        context.ChangeTracker.Entries<Expense>()
            .Single().State.Should().Be(EntityState.Deleted);

        await context.SaveChangesAsync();

        var fromDb = await context.Expenses.FindAsync(expense.Id);
        fromDb.Should().BeNull();
    }
}