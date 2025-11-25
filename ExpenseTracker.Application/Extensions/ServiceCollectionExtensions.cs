using Microsoft.Extensions.DependencyInjection;
using FluentValidation.AspNetCore;
using  FluentValidation;
using MediatR;
using ExpenseTracker.Application.Common.Behaviors;

namespace ExpenseTracker.Application.Extensions;


public static class ServiceCollectionExtensions
{
    public static void AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ServiceCollectionExtensions).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddAutoMapper(applicationAssembly);
        services.AddValidatorsFromAssembly(applicationAssembly).AddFluentValidationAutoValidation();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }
}
