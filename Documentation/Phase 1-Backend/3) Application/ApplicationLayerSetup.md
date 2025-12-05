# Application Layer Setup

The Application layer contains the use‑case logic for the system.  
It is responsible for:

- Defining commands and queries (CQRS) for each feature.
- Coordinating validation, mapping, and persistence via MediatR handlers.
- Staying independent of infrastructure concerns (no direct EF Core, logging implementations, etc.).

Below is the standard folder structure and what each part is for.
---

## Folder structure
```
Application
│
├── Common/ – Shared building blocks used across all features
│ ├── Behaviours/
│ │ └── ValidationBehavior.cs – MediatR pipeline behavior,  Executes all FluentValidation validators before the handler, short-circuits the pipeline and returns validation errors if any rules fail
│ │
│ └── Results/
│   └── Result.cs – Standard result wrapper returned by handlers
│
├── <Entity>/ – One folder per business entity/aggregate
│ ├── Commands/ – Write operations (state-changing)
│ │ ├── Create<Entity>/
│ │ │ ├── Create<Entity>Command.cs – MediatR request for creating the entity
│ │ │ ├── Create<Entity>CommandHandler.cs – Handler orchestrating the create operation
│ │ │ └── Create<Entity>CommandValidator.cs – FluentValidation rules
│ │ │
│ │ ├── Update<Entity>/
│ │ │ ├── Update<Entity>Command.cs
│ │ │ ├── Update<Entity>CommandHandler.cs
│ │ │ └── Update<Entity>CommandValidator.cs
│ │ │
│ │ └── Delete<Entity>/
│ │ ├── Delete<Entity>Command.cs
│ │ ├── Delete<Entity>CommandHandler.cs
│ │ └── Delete<Entity>CommandValidator.cs
│ │
│ ├── Dtos/
│ │ └── Create<Entity>RequestDto.cs – Request body shape for creating the entity
│ │
│ ├── Mappings/
│ │ └── <Entity>Profile.cs – AutoMapper profile for: DTOs ↔ Commands, Domain entities ↔ DTOs, Domain entities ↔ Query results
│ │
│ └── Queries/ – Read-only operations
│   └── Get<Entity>/
│     ├── Get<Entity>ByIdQuery.cs – MediatR query (contains parameters like Id, UserId)
│     ├── Get<Entity>ByIdQueryHandler.cs – Handler that fetches entity and maps to DTO/result
│     └── Get<Entity>ByIdQueryValidator.cs – FluentValidation for query parameters
│
├── Extensions/
│ └── ServiceCollectionExtensions.cs – Application-layer DI registration, Registers all MediatR handlers, Registers AutoMapper profiles, Registers FluentValidation validators, Adds pipeline behaviors (e.g., ValidationBehavior)
│
└── 
```

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
