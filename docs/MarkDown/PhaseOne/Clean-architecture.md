Clean Architecture – Notes & Standards

This document captures all decisions and patterns used to implement Clean Architecture in the Smart Expense Tracker backend.

Clean Architecture Overview

Layered Structure
Domain
Application
Infrastructure
API

Dependency Rules
	•	API → Application
	•	Infrastructure → Application
	•	Application → Domain
	•	Domain → No dependencies

Domain must never depend on any other project.

⸻

🧱 Domain Layer

Contains core business models and interfaces.

Includes:
	•	Entities (e.g., Expense)
	•	Value Objects (future)
	•	BaseEntity (Id, CreatedAt, UpdatedAt)
	•	Repository interfaces (e.g., IExpenseRepository)
	•	Domain rules (simple for Phase 1)

No:
	•	EF Core
	•	Logging
	•	Validation
	•	Mapping
	•	Services
	•	Http-related code

⸻

⚙️ Application Layer

Contains business logic.

Includes:
	•	Services (e.g., ExpenseService)
	•	DTOs
	•	Validation (FluentValidation)
	•	Mapping (AutoMapper)
	•	Interface consumption (IExpenseRepository)

No:
	•	EF Core
	•	Controllers
	•	SQL
	•	Http logic
	•	Config files

⸻

🏗️ Infrastructure Layer

Contains all technical details.

Includes:
	•	EF Core DbContext (ExpenseDbContext)
	•	Repository implementations (ExpenseRepository)
	•	SQL Server & stored proc integration
	•	Migrations
	•	Logging providers (if needed)

No:
	•	Controllers
	•	DTOs
	•	API-specific classes

⸻

🌐 API Layer

Contains:
	•	Controllers
	•	Startup / Program configuration
	•	Swagger
	•	Exception handling
	•	DI container wiring
	•	Logging middleware

Calls the Application layer → never directly calls Infrastructure.

⸻

🔌 Dependency Injection Map

API → Application
API → Infrastructure
Application → Domain
Infrastructure → Application


📝 Coding Decisions for This Project

Entities
	•	Use Guid IDs
	•	Use BaseEntity for shared fields
	•	Keep domain light (Phase 1)

Repositories
	•	Interface in Domain
	•	Implementation in Infrastructure
	•	All async with CancellationTokens

DTOs
	•	Defined in Application
	•	AutoMapper used for mapping

Validation
	•	Use FluentValidation in Application layer

Logging
	•	Structured logging
	•	Logging middleware in API layer

Health Checks
	•	Exposed at /health