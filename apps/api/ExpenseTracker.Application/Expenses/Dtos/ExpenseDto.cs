namespace ExpenseTracker.Application.Expenses.Dtos;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "CAD";
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}