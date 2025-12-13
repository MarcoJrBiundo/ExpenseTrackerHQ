using AutoMapper;
using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Expenses.Dtos;
using ExpenseTracker.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Expenses.Queries.GetExpenseByIdQuery;

public sealed class GetExpenseByIdQueryHandler(
    ILogger<GetExpenseByIdQueryHandler> logger,
    IMapper mapper,
    IExpensesRepository expenseRepository)
    : IRequestHandler<GetExpenseByIdQuery, Result<ExpenseDto?>>
{
    private readonly ILogger<GetExpenseByIdQueryHandler> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IExpensesRepository _expenseRepository = expenseRepository;

    
    public async Task<Result<ExpenseDto?>> Handle(
        GetExpenseByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GetExpenseByIdQuery for UserId: {UserId}, ExpenseId: {ExpenseId}",
            request.UserId, request.ExpenseId);

        var expense = await _expenseRepository
            .GetExpenseByIdAsync(request.UserId, request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            // Could be: not found OR not owned by this user.
            _logger.LogWarning(
                "Expense not found or not accessible for UserId: {UserId}, ExpenseId: {ExpenseId}",
                request.UserId, request.ExpenseId);

            return Result<ExpenseDto>.Fail("Expense not found.");
        }

        _logger.LogInformation(
            "Retrieved expense for UserId: {UserId}, ExpenseId: {ExpenseId}",
            request.UserId, request.ExpenseId);
         return Result<ExpenseDto>.Ok(_mapper.Map<ExpenseDto>(expense));
    }
}