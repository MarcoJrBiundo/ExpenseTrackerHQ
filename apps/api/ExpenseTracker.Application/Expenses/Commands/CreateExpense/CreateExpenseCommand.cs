using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Expenses.Dtos;
using ExpenseTracker.Domain.Entities;
using MediatR;

namespace ExpenseTracker.Application.Expenses.Commands.CreateExpense;

public record CreateExpenseCommand(
    Guid UserId,
    decimal Amount,
    string Currency,
    string Category,
    DateTime Date,
    string? Description
) : IRequest<Result<ExpenseDto>>;