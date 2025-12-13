using System;
using System.Linq;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Repositories;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Repositories;

public class ExpensesRepository : IExpensesRepository
{
    private readonly ExpenseDbContext _dbContext;

    public ExpensesRepository(ExpenseDbContext dbContext)
    {
        _dbContext = dbContext; 
    }
    public async Task<Guid> AddExpenseAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        if (expense == null) throw new ArgumentNullException(nameof(expense));

        if (expense.Id == Guid.Empty)
        {
            expense.Id = Guid.NewGuid();
        }

        await _dbContext.Expenses.AddAsync(expense, cancellationToken);
        return expense.Id;
    }

    public Task DeleteExpense(Expense expense, CancellationToken cancellationToken = default)
    {
        _dbContext.Expenses.Remove(expense);
        return Task.CompletedTask;
    }

    public Task<Expense?> GetExpenseByIdAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default)
    {
       return _dbContext.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<Expense>> GetExpensesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
       IReadOnlyList<Expense> expense = await _dbContext.Expenses.Where(e => e.UserId == userId).AsNoTracking().ToListAsync(cancellationToken);
       return expense; 
    }

    public async Task<IReadOnlyList<Expense>> GetExpensesByUserViaStoredProcAsync( Guid userId, CancellationToken cancellationToken = default)
{
    var userIdParam = new SqlParameter("@UserId", userId);

    var query = _dbContext.Expenses
        .FromSqlRaw("EXEC [dbo].[sp_GetExpensesByUser] @UserId", userIdParam)
        .AsNoTracking();

    return await query.ToListAsync(cancellationToken);
}
}
