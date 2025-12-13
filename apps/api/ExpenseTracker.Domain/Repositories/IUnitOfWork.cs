
namespace ExpenseTracker.Domain.Repositories;

public interface IUnitOfWork
{
    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}   