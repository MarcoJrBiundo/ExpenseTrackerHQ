# Application Layer Setup

The Application layer contains the use‑case logic for the system.  
It is responsible for:

- Defining commands and queries (CQRS) for each feature.
- Coordinating validation, mapping, and persistence via MediatR handlers.
- Staying **independent** of infrastructure concerns (no direct EF Core, logging implementations, etc.).

Below is the standard folder structure and what each part is for.

---

## Folder structure

`Common` - Shared building blocks used across all features.
- `Behaviours`
  - `ValidationBehavior.cs`  
    - A MediatR pipeline behavior that:
      - Runs all FluentValidation validators for a request **before** it reaches the handler.
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

`Extensions`- Typically you will have an `Extensions` folder (or a `DependencyInjection.cs` in the root of the Application project) that:

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