## API Layer Setup

The API (Presentation) Layer is the entry point of the entire application.  
It exposes HTTP endpoints, accepts incoming requests, validates and translates data, and delegates all business operations to the Application layer.  
It should contain zero business logic and zero data access logic — its responsibility is purely presentation, routing, and orchestration.


## What the API Layer Contains

- Controllers
  - Define HTTP endpoints
  - Bind request parameters (route, query, body)
  - Send commands/queries to the Application Layer (e.g., via MediatR)
  - Return appropriate HTTP responses and status codes

- **Middlewares**
  - Handle cross‑cutting concerns such as error handling, logging, authentication, or request tracking
  - Ensure consistent API behavior across all endpoints

- **Configuration Files**
  - Environment-based settings such as connection strings, logging settings, or feature flags

- **Program.cs**
  - Bootstraps the application
  - Configures DI, middleware pipeline, routing, Swagger, etc.

- **Launch Settings**
  - Defines local debugging profiles and environment variables

- **Dockerfile**
  - Defines how the API is containerized

The API layer acts as the “face” of the backend—clean, predictable, and stable.

---

## Folder Structure

```
Api
│
├── Controllers/ - Contains all HTTP endpoints for the API
│     └── EntityController.cs - Defines routes for CRUD operations, Uses MediatR to send commands/queries to the Application layer
│
├── Middleware/
│     ├── ErrorHandlingMiddleware.cs -  Global exception handling, Converts exceptions into ProblemDetails responses, Ensures consistent error format across all endpoints
│
├── Properties/
│     └── LaunchSettings.json - Local development settings, Profiles for running the API with IIS Express or Kestrel
│
├── Appsettings/
│     └── Appsettings.Dev.json - Environment-specific configuration (Dev), Logging settings, API settings, connection strings, etc.
│
├── Dockerfile
│     - Defines container build steps for deploying the API
│     - Installs .NET runtime, exposes ports, copies application files
│
└── Program.cs -  Application entry point, Configures services (AddControllers, Swagger, CORS, MediatR, etc.),Sets up middleware pipeline (UseRouting, UseExceptionHandling, UseSwaggerUI)
```

---

## Required NuGet Packages
- `Microsoft.AspNetCore.Mvc`  
- `MediatR.Extensions.Microsoft.DependencyInjection`  
- `Swashbuckle.AspNetCore`  
- `Microsoft.Extensions.Configuration.*`  
- `Microsoft.AspNetCore.Authentication.JwtBearer` (if using JWT auth)
- `FluentValidation.AspNetCore` (if adding validation pipelines)

---


## Program.cs Example
```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(); 
builder.Services.AddFluentValidationAutoValidation();     
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration );
builder.Services.AddApplication();

builder.Host.UseSerilog((context, configuration) => configuration .ReadFrom.Configuration(context.Configuration));


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EntityDbContext>();
    dbContext.Database.Migrate(); 
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
```



## EntityController.cs Example
```c# 
{
    [ApiController]
    [Route("api/v1/users/{userId:guid}/entitys")]
    public class EntityController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EntityController(IMediator mediator){_mediator = mediator;}

        [HttpGet]
        public async Task<IActionResult> GetEntitysByUserIdAsync([FromRoute]Guid userId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntitysByUserQuery(userId), cancellationToken);
            if (!result.Success)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpGet("{entityId:guid}", Name = "GetEntityById")]
        public async Task<IActionResult> GetEntityByIdAsync([FromRoute] Guid userId, [FromRoute] Guid entityId,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntityByIdQuery(userId, entityId), cancellationToken);
            if (!result.Success)
                return NotFound(result.ErrorMessage);
            return Ok(result.Data);
        }   
      
        [HttpPost]
        public async Task<IActionResult> CreateEntityAsync([FromRoute] Guid userId,[FromBody] CreateEntityRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CreateEntityCommand(UserId,request.Amount,request.Currency,request.Category,request.Date,request.Description),cancellationToken);
            if (!result.Success)
                return BadRequest(result.ErrorMessage);
            return CreatedAtRoute(routeName: "GetEntityById", routeValues: new { userId, entityId = result.Data!.Id },value: result.Data);
        }

    }
}

```

## ErrorHandlingMiddleware.cs Example
```c#
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try{ await _next(context);}
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var problem = new { type = "https://httpstatuses.com/500", title = "An unexpected error occurred.", status = statusCode, detail = exception.Message, traceId = context.TraceIdentifier};

        var json = JsonSerializer.Serialize(problem);
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsync(json);
    }
}
```

## DockerFile Example
```dockerfile
#STAGE ONE
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# 1. Set working directory inside the container
WORKDIR /src
# 2. Copy csproj files first (leverages Docker layer caching for faster builds)
COPY ["EntityTracker.Api/EntityTracker.Api.csproj", "EntityTracker.Api/"]
COPY ["EntityTracker.Application/EntityTracker.Application.csproj", "EntityTracker.Application/"]
COPY ["EntityTracker.Infrastructure/EntityTracker.Infrastructure.csproj", "EntityTracker.Infrastructure/"]
COPY ["EntityTracker.Domain/EntityTracker.Domain.csproj", "EntityTracker.Domain/"]
# 3. Restore dependencies
RUN dotnet restore "EntityTracker.Api/EntityTracker.Api.csproj"
# 4. Copy the rest of the source code
COPY . .
# 5. Move into the API project folder
WORKDIR /src/EntityTracker.Api
# 6. Publish the app (Release, self-contained trimmed app disabled, generate only DLL)
RUN dotnet publish "EntityTracker.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false
# STAGE 2: runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
# 7. Set working directory for the runtime container
WORKDIR /app
# 8. Copy published output from build stage
COPY --from=build /app/publish .
# 9. Configure port inside container (we'll map it to a host port when running)
ENV ASPNETCORE_URLS=http://+:8080
# 10. Optional: set environment (can override with -e at runtime)
ENV ASPNETCORE_ENVIRONMENT=Development
# 11. Expose the port (for documentation only; docker run -p is what really matters)
EXPOSE 8080
# 12. Entrypoint: run the API
ENTRYPOINT ["dotnet", "EntityTracker.Api.dll"]
```
