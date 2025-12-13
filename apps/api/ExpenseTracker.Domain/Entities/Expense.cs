using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities;

public class Expense : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "CAD";
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}