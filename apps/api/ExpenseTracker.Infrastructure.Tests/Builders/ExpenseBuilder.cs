using System;
using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Infrastructure.Tests.Builders;

public class ExpenseBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private decimal _amount = 10m;
    private string _currency = "CAD";
    private string _category = "General";
    private DateTime _date = DateTime.UtcNow.Date;
    private string? _description = "Test expense";

    public ExpenseBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ExpenseBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public ExpenseBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public ExpenseBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public ExpenseBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public ExpenseBuilder WithDate(DateTime date)
    {
        _date = date;
        return this;
    }

    public ExpenseBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public Expense Build()
    {
        return new Expense
        {
            Id = _id,
            UserId = _userId,
            Amount = _amount,
            Currency = _currency,
            Category = _category,
            Date = _date,
            Description = _description
        };
    }
}