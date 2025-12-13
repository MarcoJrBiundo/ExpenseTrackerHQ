namespace ExpenseTracker.Application.Expenses.Mappings;

using AutoMapper;
using ExpenseTracker.Application.Expenses.Commands.CreateExpense;
using ExpenseTracker.Application.Expenses.Dtos;
using ExpenseTracker.Domain.Entities;
public class ExpensesProfile : Profile
{
    public ExpensesProfile()
    {
        CreateMap<Expense, ExpenseDto>();
        CreateMap<CreateExpenseCommand, Expense>();
        CreateMap<Expense, CreateExpenseCommand>();

    }
}