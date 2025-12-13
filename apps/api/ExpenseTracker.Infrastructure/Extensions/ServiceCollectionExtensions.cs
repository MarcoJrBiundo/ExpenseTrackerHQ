using System.IO.Compression;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ExpenseTracker.Domain.Repositories;
using ExpenseTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Infrastructure.Persistence;

namespace ExpenseTracker.Infrastructure.Extensions;

        
public static class ServiceCollectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration   configuration)
    {

        var connectionString = configuration.GetConnectionString("ExpenseTrackerDb")
            ?? throw new InvalidOperationException("Connection string 'ExpenseTrackerDb' not found.");

       services.AddDbContext<ExpenseDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        
        services.AddScoped<IExpensesRepository, ExpensesRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}       