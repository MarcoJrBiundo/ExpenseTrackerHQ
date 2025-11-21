
using ExpenseTracker.Application.Expenses.Commands.CreateExpense;
using ExpenseTracker.Application.Expenses.Commands.DeleteExpense;
using ExpenseTracker.Application.Expenses.Commands.UpdateExpense;
using ExpenseTracker.Application.Expenses.Dtos;
using ExpenseTracker.Application.Expenses.Queries.GetExpenseByIdQuery;
using ExpenseTracker.Application.Expenses.Queries.GetExpensesByUserQuery;
using MediatR;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseController(IMediator mediator) : ControllerBase
    {
        
        
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetExpensesByUserIdAsync(Guid userId)
        {
              var expenses = await mediator.Send(new GetExpensesByUserQuery(userId));
             return Ok(expenses);
        }

        
        [HttpGet("{userId}/{expenseId}")]
        public async Task<IActionResult> GetExpenseByIdAsync(Guid userId, Guid expenseId)
        {
             var expense = await mediator.Send(new GetExpenseByIdQuery(userId, expenseId));
             return Ok(expense);
        }   
      
        [HttpPost]
        public async Task<IActionResult> CreateExpenseAsync([FromBody] CreateExpenseRequestDto request)
        {
            var expense = await mediator.Send(new CreateExpenseCommand(  request.UserId,
                request.Amount,
                request.Currency,
                request.Category,
                request.Date,
                request.Description ));
             return Ok(expense);
        }
       
        [HttpPut("{expenseId}")]
        public  async Task<IActionResult> UpdateExpense(int expenseId, [FromBody] ExpenseDto request)
        {
            var expense = await mediator.Send(new UpdateExpenseCommand(  request.UserId,
            request.Id,
            request.Amount,
            request.Currency,
            request.Category,
            request.Description,
            request.Date));
             return Ok(expense);
        }
     

        [HttpDelete("{expenseId}")]
        public async Task<IActionResult> DeleteExpense(Guid userId, Guid expenseId)
        {
             var expense = await mediator.Send(new DeleteExpenseCommand(userId, expenseId));
             return Ok(expense);
        }
    }
}
