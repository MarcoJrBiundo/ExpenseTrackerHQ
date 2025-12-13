
using ExpenseTracker.Application.Expenses.Commands.CreateExpense;
using ExpenseTracker.Application.Expenses.Commands.DeleteExpense;
using ExpenseTracker.Application.Expenses.Commands.UpdateExpense;
using ExpenseTracker.Application.Expenses.Dtos;
using ExpenseTracker.Application.Expenses.Queries.GetExpenseByIdQuery;
using ExpenseTracker.Application.Expenses.Queries.GetExpensesByUserQuery;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers
{
    [ApiController]
    [Route("api/v1/users/{userId:guid}/expenses")]
    public class ExpenseController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExpenseController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetExpensesByUserIdAsync([
            FromRoute]Guid userId, 
            CancellationToken cancellationToken)
        {
             var result = await _mediator.Send(new GetExpensesByUserQuery(userId), cancellationToken);

            if (!result.Success)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpGet("{expenseId:guid}", Name = "GetExpenseById")]
        public async Task<IActionResult> GetExpenseByIdAsync(
            [FromRoute] Guid userId, 
            [FromRoute] Guid expenseId, 
            CancellationToken cancellationToken)
        {
             var result = await _mediator.Send(new GetExpenseByIdQuery(userId, expenseId), cancellationToken);
            
            if (!result.Success)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }   
      
        [HttpPost]
        public async Task<IActionResult> CreateExpenseAsync(
            [FromRoute] Guid userId,
            [FromBody] CreateExpenseRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CreateExpenseCommand(
                    userId,
                    request.Amount,
                    request.Currency,
                    request.Category,
                    request.Date,
                    request.Description),
                cancellationToken);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return CreatedAtRoute(
                routeName: "GetExpenseById",               
                routeValues: new { userId, expenseId = result.Data!.Id },
                value: result.Data);
        }

        [HttpPut("{expenseId:guid}")]
        public  async Task<IActionResult> UpdateExpense(
           [FromRoute] Guid userId, 
           [FromRoute] Guid expenseId,
           [FromBody] ExpenseDto request,
           CancellationToken cancellationToken)
        {

            if (request.Id != Guid.Empty && request.Id != expenseId)
            {
                return BadRequest("Route expenseId does not match body Id.");
            }

            var command = new UpdateExpenseCommand(
                UserId: userId,
                ExpenseId: expenseId,
                Amount: request.Amount,
                Currency: request.Currency,
                Category: request.Category,
                Description: request.Description,
                Date: request.Date);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return NotFound(result.ErrorMessage);
            }

            return NoContent();
        }
     
    [HttpDelete("{expenseId:guid}")]
        public async Task<IActionResult> DeleteExpense(
            [FromRoute] Guid userId,
            [FromRoute] Guid expenseId,
            CancellationToken cancellationToken)
        {
            var command = new DeleteExpenseCommand(userId, expenseId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return NotFound(result.ErrorMessage);
            }
            return NoContent();
        }

    }
}
