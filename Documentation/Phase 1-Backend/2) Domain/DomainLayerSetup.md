## Domain Layer Setup

## 📁 Folder Structure
```
Domain
 ├── Common
 │    └── BaseEntity.cs
 ├── Entities
 │    └── // Business entity classes (e.g., Expense, User, Category)
 ├── Repositories
 │    └── // Repository interfaces (one per aggregate root)
	  └── IUnitOfWork.cs
```

## Set Up Base Entity 
```csharp
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

		public void SetCreated(DateTime utcNow)
        {
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public void SetUpdated(DateTime utcNow)
        {
            UpdatedAt = utcNow;
        }
    }
```

## Why is Base Entity set up this way ?

BaseEntity is an abstract domain base class that all entities inherit from. It standardizes core fields and behavior across the domain.

## Purpose
	•	Provide a consistent identity (Id primary key as a Guid)
	•	Provide automatic auditing (CreatedAt, UpdatedAt)
	•	Encapsulate timestamp-setting logic via SetCreated and SetUpdated
	•	Ensure all domain models follow the same structure

## Why abstract
	•	Prevents direct instantiation of BaseEntity
	•	Ensures it is only used as a superclass
	•	Allows future abstract members that children must implement

## Set up Entities - Self explanatory, just make sure they inherit from BaseEntity.

## Set up interfaces for repositories, one per entity.

## Set up one IUnitOfWork interface.

This should typically expose a single SaveChangesAsync (or SaveChanges) method, representing an atomic commit of all pending changes. Repositories should focus on manipulating aggregates (adding, removing, updating entities), while the UnitOfWork coordinates the final commit by calling the underlying DbContext. The DbContext can then handle setting CreatedAt and UpdatedAt for all tracked entities in one place (e.g., in an overridden SaveChangesAsync).

---

## Domain Layer Dependency Rule

The Domain layer must remain **100% independent** of all other layers.  
It should not depend on:

- Application layer  
- Infrastructure layer  
- API / Presentation layer  
- EF Core  
- ASP.NET Core  
- Serilog  
- MediatR  
- AutoMapper  
- Any external NuGet packages (except for small pure C# utility packages)

The Domain layer contains only **business rules**, **entities**, **value objects**, and pure C# logic.

---

## Value Objects (Optional but Recommended)

Value Objects are immutable domain types that:

- Have no identity (no Id)  
- Compare by value rather than by reference  
- Are always valid upon creation  
- Live exclusively inside the Domain layer

Example:

```csharp
public sealed record Money(decimal Amount, string Currency);
```

---

## Entities vs Aggregate Roots

- **Entities**: Domain objects with an identity.  
- **Aggregate Roots**: Specific entities that act as entry points to a cluster of related objects.  
  Only Aggregate Roots should have repositories.

Example:  
`Expense` would typically be considered an aggregate root.

---

## Why Repository Interfaces Live in the Domain

Repository interfaces belong in the Domain layer because:

- They define **persistence contracts**, not persistence implementations  
- Application layer consumes the interfaces  
- Infrastructure layer provides the implementations  
- Domain remains persistence-agnostic and independent of EF Core or SQL

---

## Domain Layer Checklist

- [x] BaseEntity defined  
- [x] Domain entities inherit from BaseEntity  
- [x] Repository interfaces added (one per aggregate root)  
- [x] IUnitOfWork defined  
- [x] No external dependencies  
- [x] Value objects created when needed  
- [x] No EF, SQL, HTTP, or logging dependencies  
- [x] Pure business rules only  
