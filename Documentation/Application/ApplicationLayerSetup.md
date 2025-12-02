# Application Layer Setup

The Application layer contains the use‑case logic for the system.  
It is responsible for:

- Defining commands and queries (CQRS) for each feature.
- Coordinating validation, mapping, and persistence via MediatR handlers.
- Staying independent of infrastructure concerns (no direct EF Core, logging implementations, etc.).

Below is the standard folder structure and what each part is for.
---

## Folder structure

`Common` - Shared building blocks used across all features.
- `Behaviours`
  - `ValidationBehavior.cs`  
    - A MediatR pipeline behavior that:
      - Runs all FluentValidation validators for a request before it reaches the handler.
      - Short‑circuits the pipeline and returns a validation error result if any rules fail.
- `Results`
  - `Result.cs` - A simple result wrapper used by handlers, e.g.:


`<Entity>`-(one folder per business entity / aggregate) Replace `<Entity>` with the actual entity name,

`Commands/`- Commands represent **operations that change state** (write operations). - For each command, create a dedicated folder:
  - `Delete<Entity>/`
    - `Delete<Entity>Command.cs` - The MediatR request that contains the data needed to delete the entity
    - `Delete<Entity>CommandHandler.cs`- The MediatR handler that coordinates the delete operation using repositories
    - `Delete<Entity>CommandValidator.cs`FluentValidation validator for the command (e.g. required IDs, valid formats).
  - `Update<Entity>/`
    - `Update<Entity>Command.cs`
    - `Update<Entity>CommandHandler.cs`
    - `Update<Entity>CommandValidator.cs`
  - `Create<Entity>/`
    - `Create<Entity>Command.cs`
    - `Create<Entity>CommandHandler.cs`
    - `Create<Entity>CommandValidator.cs`
- `Dtos/`
  - `Create<Entity>RequestDto.cs`- Shape of the request body for creating an entity.

`Mappings`- AutoMapper profiles that map between:
- DTOs ↔ Commands
- Domain entities ↔ DTOs
- Domain entities ↔ Query results
- `Mappings/`
  - `<Entity>Profile.cs`  
    - An AutoMapper `Profile` that configures all mappings related to `<Entity>`.

`Queries` - Queries represent **read‑only operations** (no state changes). For each query, create a dedicated folder:
- `Queries/`
  - `Get<Entity>/`
    - `Get<Entity>ByIdQuery.cs`  - The MediatR request object (e.g. contains `Id`, `UserId`).
    - `Get<Entity>ByIdQueryHandler.cs` - The MediatR handler that reads data (via repository) and maps it to a DTO or result.
    - `Get<Entity>ByIdQueryValidator.cs`- FluentValidation validator for query parameters.

`Extensions`- Typically you will have an `Extensions` folder (or a `ServiceCollectionExtensions.cs` in the root of the Application project) that:

- Registers MediatR handlers from this assembly.
- Registers AutoMapper profiles from this assembly.
- Registers FluentValidation validators.
- Adds pipeline behaviors like `ValidationBehavior`.



---
## Required NuGet packages
- `MediatR`- For defining requests/handlers and implementing CQRS.
- `AutoMapper`- For mapping between entities, DTOs, and command/query objects.
- `FluentValidation.AspNetCore`- For implementing validation rules on commands and queries.
- `Microsoft.Extensions.Logging.Abstractions` -  For logging abstractions used in handlers (the concrete implementation lives in the outer layers).


- `ValidationBehavior.cs` Example(This is standard and unlikely to change app to app) 
```c#
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators){_validators = validators;}

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            if (!result.IsValid)
                failures.AddRange(result.Errors);
        }
        if (failures.Count != 0)
            throw new ValidationException(failures);
        return await next();
    }
}
```
`Result.cs` - Example(This is standard and unlikely to change app to app)  
```c#
public class Result
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public static Result Ok() => new() { Success = true };
    public static Result Fail(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

public class Result<T>
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public T? Data { get; init; }
    public static Result<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };
    public static Result<T> Fail(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}
```

Adding a CQRS Example 

```c#
public record CreateEntityCommand(Guid UserId, decimal Amount,string Currency, string Category,DateTime Date,string? Description) : IRequest<Result<EntityDto>>;

---

public sealed class DeleteEntityCommandHandler(IEntityRepository entityRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteEntityCommand, Result>
{
    private readonly IEntityRepository _entityRepository = EntityRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> Handle(DeleteEntityCommand request, CancellationToken cancellationToken)
    {
        var entity = await _entityRepository.GetEntityIdAsync(request.UserId, request.EntityId, cancellationToken);

        if (entity is null)
             return Result.Fail("Entity not found.");

        await entityRepository.DeleteEEntity(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
---

public class CreateEntityCommandValidator : AbstractValidator<CreateEntityCommand>
{
   public CreateEntityCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0) .WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required.").Length(3).WithMessage("Currency must be a 3-letter code.");
        RuleFor(x => x.Category).NotEmpty().WithMessage("Category is required.").MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(250);
        RuleFor(x => x.Date).LessThanOrEqualTo(DateTime.UtcNow) .WithMessage("Date cannot be in the future.").GreaterThan(DateTime.UtcNow.AddYears(-5)).WithMessage("Date must be within the last 5 years.");
    }
}

```

`ServiceCollectionExtensions` - Example(This will be added to program.cs, things might change but these 5 lines will essentialy be constant)
```c#
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
```
`Mapping Exmaple`
```c#
public class EntityProfile : Profile
{
    public EntityProfile()
    {
        CreateMap<Entity, EntityDto>();
        CreateMap<CreateEntityCommand, Entity>();
        CreateMap<Entity, CreateEntityCommand>();

    }
}
```