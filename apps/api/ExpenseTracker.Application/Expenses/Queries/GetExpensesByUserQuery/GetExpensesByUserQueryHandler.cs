namespace ExpenseTracker.Application.Expenses.Queries.GetExpensesByUserQuery;

using ExpenseTracker.Application.Expenses.Dtos;
using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ExpenseTracker.Domain.Repositories;
using ExpenseTracker.Application.Common.Results;

public sealed class GetExpensesByUserQueryHandler(
    ILogger<GetExpensesByUserQueryHandler> logger, 
    IMapper mapper, 
    IExpensesRepository expenseRepository) 
    : IRequestHandler<GetExpensesByUserQuery, Result<IEnumerable<ExpenseDto>>>
{
    private readonly ILogger<GetExpensesByUserQueryHandler> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IExpensesRepository _expenseRepository = expenseRepository;

    public async Task<Result<IEnumerable<ExpenseDto>>> Handle(
        GetExpensesByUserQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GetExpensesByUserQuery for UserId: {UserId}",
            request.UserId);

        var expenses = (await _expenseRepository
            .GetExpensesByUserAsync(request.UserId, cancellationToken))
            .ToList();

        //Alternative method using stored procedure
        // var expenses = (await _expenseRepository
        //     .GetExpensesByUserViaStoredProcAsync(request.UserId, cancellationToken))
        //     .ToList();    

        _logger.LogInformation(
            "Retrieved {Count} expenses for UserId: {UserId}",
            expenses.Count, request.UserId);

        var expenseDtos = _mapper.Map<IEnumerable<ExpenseDto>>(expenses);

        return Result<IEnumerable<ExpenseDto>>.Ok(expenseDtos);
    }
}