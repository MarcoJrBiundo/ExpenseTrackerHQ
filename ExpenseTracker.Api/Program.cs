

using ExpenseTracker.Infrastructure.Extensions;
using ExpenseTracker.Application.Extensions;
using ExpenseTracker.API.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<ErrorHandlingMiddle>();


builder.Services.AddInfrastructure(builder.Configuration );
builder.Services.AddApplication();

builder.Host.UseSerilog((context, configuration) =>
    configuration
      .ReadFrom.Configuration(context.Configuration) 
);


var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddle>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
