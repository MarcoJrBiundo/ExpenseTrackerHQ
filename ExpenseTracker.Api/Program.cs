

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

builder.Host.UseSerilog((context, configuration) =>
    configuration
      .ReadFrom.Configuration(context.Configuration)  
);


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
    dbContext.Database.Migrate(); // creates ExpenseTrackerDb and tables if they don't exist
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
app.MapControllers();
app.Run();
