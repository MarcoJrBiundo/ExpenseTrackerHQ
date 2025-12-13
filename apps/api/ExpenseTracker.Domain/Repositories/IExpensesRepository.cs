using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Domain.Repositories;

public interface IExpensesRepository
{
    Task<IEnumerable<Expense>> GetExpensesByUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Expense>> GetExpensesByUserViaStoredProcAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<Expense?> GetExpenseByIdAsync(
        Guid userId,
        Guid expenseId, 
        CancellationToken cancellationToken = default);

    Task<Guid> AddExpenseAsync(
        Expense expense, CancellationToken cancellationToken = default);
        
    Task DeleteExpense(
        Expense expense, 
        CancellationToken cancellationToken = default);
}