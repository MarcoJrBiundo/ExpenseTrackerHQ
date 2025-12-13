using FluentValidation;

namespace ExpenseTracker.Application.Expenses.Commands.UpdateExpense;


public class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
     public UpdateExpenseCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.ExpenseId)
            .NotEmpty().WithMessage("ExpenseId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be a positive number.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter code.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(250);

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Date cannot be in the future.")
            .GreaterThan(DateTime.UtcNow.AddYears(-5))
            .WithMessage("Date must be within the last 5 years.");
    }
}