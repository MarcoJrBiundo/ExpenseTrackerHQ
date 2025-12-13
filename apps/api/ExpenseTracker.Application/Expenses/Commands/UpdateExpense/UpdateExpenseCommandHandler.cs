using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Expenses.Commands.UpdateExpense;

public sealed class UpdateExpenseCommandHandler(ILogger<UpdateExpenseCommandHandler> logger, IExpensesRepository expenseRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateExpenseCommand, Result>
{
    private readonly ILogger<UpdateExpenseCommandHandler> _logger = logger;
    private readonly IExpensesRepository _expenseRepository = expenseRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {

        _logger.LogInformation(
            "Handling UpdateExpenseCommand for UserId: {UserId}, ExpenseId: {ExpenseId}",
            request.UserId,
            request.ExpenseId);

        var expense = await _expenseRepository.GetExpenseByIdAsync(request.UserId, request.ExpenseId, cancellationToken);
        if (expense == null)
        {
            _logger.LogWarning(
                "Expense not found or not accessible for UserId: {UserId}, ExpenseId: {ExpenseId}",
                request.UserId,
                request.ExpenseId);
            return Result.Fail("Expense not found.");
        }

        expense.Amount = request.Amount;
        expense.Currency = request.Currency;
        expense.Category = request.Category;
        expense.Description = request.Description;
        expense.Date = request.Date;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully updated ExpenseId: {ExpenseId} for UserId: {UserId}",
            request.ExpenseId,
            request.UserId);
            
        return Result.Ok();
    }
}