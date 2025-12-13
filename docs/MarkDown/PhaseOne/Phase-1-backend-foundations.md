Phase 1 — Backend Foundations

🎯 Goal

Build a production-ready .NET 8 Clean Architecture API with EF Core, SQL Server, health checks, validation, mapping, and a full Docker Compose environment.

This phase establishes the core backend patterns that every future phase relies on.

⸻

🧩 Epic: Phase 1 – Backend Foundations

Description:
Build a single-service Expense Tracker API using Clean Architecture in .NET 8.
Run the API locally in Docker Compose with SQL Server, async EF Core, and proper application layers.

⸻

Feature: Clean Architecture & Core Domain

Description:
Establish the Clean Architecture folder structure and implement the Expense domain, DTOs, validation, mapping, logging, and health endpoints.

Stories & Tasks

⸻

Story: Complete Clean Architecture Course

Tasks:
	•	Complete Udemy course “ASP.NET Core 8 Web API: Clean Architecture + Azure Services”
	•	Add notes to docs/backend/clean-architecture.md
	•	Review architecture layering decisions
	•	Document final folder structure

⸻

Story: Setup project structure

Tasks:
	•	Create solution ExpenseTracker.sln
	•	Add Domain, Application, Infrastructure, Api projects
	•	Add project references (Domain → Application → Infrastructure → Api)
	•	Configure appsettings.Development.json
	•	Add Swagger
	•	Add basic logging middleware
	•	Add GET /health endpoint

⸻

Story: Define core domain model

Tasks:
	•	Create Expense entity
	•	Create BaseEntity (Id, CreatedAt, UpdatedAt)
	•	Add IExpenseRepository interface

⸻

Story: Implement Application layer

Tasks:
	•	Create DTOs (ExpenseDto, CreateExpenseRequest)
	•	Add ExpenseService with async methods
	•	Add FluentValidation (ExpenseValidator)
	•	Add AutoMapper Profile (ExpenseProfile)

⸻

Story: Expose API endpoints

Tasks:
	•	Create ExpenseController
	•	POST /api/expenses
	•	GET /api/expenses/{userId}
	•	Consider MediatR usage (optional)
	•	Test endpoints via Swagger

⸻

Story: Application layer tests

Tasks:
	•	Add xUnit test project
	•	Write tests for ExpenseService
	•	Add basic CI test workflow (placeholder)

⸻

Feature: Entity Framework Core (Async + Repository Pattern)

Story: Complete EF Core Course
	•	Finish Udemy “Entity Framework Core – The Complete Guide (2024)”
	•	Add notes to docs/backend/ef-core-notes.md

Story: Setup EF Core
	•	Install EF Core packages
	•	Add ExpenseDbContext
	•	Configure DbSet<Expense>
	•	Add InMemory provider for tests

Story: Implement Repository
	•	Add ExpenseRepository (EF Core implementation)
	•	Implement AddExpenseAsync, GetExpensesByUserAsync, etc.
	•	Add integration tests

Story: Async + Cancellation Tokens
	•	Ensure all service and repo methods use async/await
	•	Add CancellationToken parameters
	•	Test cancellation behavior

⸻

Feature: Local SQL Integration (Stored Procedures)

Story: Complete Azure SQL Course
	•	Finish Udemy “Azure SQL for Developers”
	•	Write notes in docs/sql/notes.md

Story: Configure SQL Server
	•	Run SQL Server locally (container or local install)
	•	Create ExpenseTrackerDB
	•	Create Expenses table

Story: Create Stored Procedures
	•	sp_AddExpense
	•	sp_GetExpensesByUser
	•	Test via Azure Data Studio

Story: Repository Integration
	•	Call stored procedures with FromSqlRaw
	•	Map results to domain models
	•	Validate via Swagger

⸻

Feature: Docker Compose Local Environment

Story: Complete Docker Course
	•	Finish “Docker & Kubernetes: The Practical Guide”
	•	Add notes to docs/docker/notes.md

Story: Containerize API
	•	Create multi-stage Dockerfile
	•	Build image
	•	Confirm app runs in container

Story: Setup Docker Compose
	•	Create docker-compose.yml
	•	Define api, db, and ui services
	•	Configure ports, volumes, env vars

Story: Validate Environment
	•	docker compose up
	•	Check API at localhost:5000
	•	Validate DB connection
	•	Check logs and health endpoint

⸻

Success Criteria

Phase 1 is complete when:
	•	The API runs locally using Docker Compose
	•	Expense creation + querying works
	•	Clean Architecture layers are in place
	•	Repository + EF Core are working
	•	Logging + health checks work
	•	Everything is documented
