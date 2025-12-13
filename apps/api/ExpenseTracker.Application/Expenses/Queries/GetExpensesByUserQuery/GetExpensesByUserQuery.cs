using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Expenses.Dtos;
using ExpenseTracker.Domain.Entities;
using MediatR;

namespace ExpenseTracker.Application.Expenses.Queries.GetExpensesByUserQuery;

public record  GetExpensesByUserQuery(Guid UserId) : IRequest<Result<IEnumerable<ExpenseDto>>>;
    