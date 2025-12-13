

using ExpenseTracker.Infrastructure.Extensions;
using ExpenseTracker.Application.Extensions;
using Serilog;
using FluentValidation.AspNetCore;
using ExpenseTracker.Api.Middlewares;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(); 
builder.Services.AddFluentValidationAutoValidation();     
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration );
builder.Services.AddApplication();
builder.Services.AddHealthChecks();

builder.Host.UseSerilog((context, configuration) =>
    configuration
      .ReadFrom.Configuration(context.Configuration)  
);


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
    var runMigrations = builder.Configuration.GetValue<bool>("RunMigrations");
    if (runMigrations)
    {  
        dbContext.Database.Migrate();
    }
}

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
