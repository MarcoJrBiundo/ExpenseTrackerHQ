# EF Core Commands (Reference Guide)
The commands assume:
- Your Infrastructure project contains the `DbContext`
- Your API project is the startup project
- You are inside the root of the solution (or at least the API project folder)
You can adapt the paths using `--project` and `--startup-project` if needed.
---

# 1️⃣ Add a New Migration  -Creates a migration file based on current DbContext and entity changes.
```bash
dotnet ef migrations add InitialCreate --project ../ExpenseTracker.Infrastructure --startup-project .
```
**Explanation:**
- `migrations add InitialCreate` → Name of the migration  
- `--project` → Points to the project *where the DbContext lives*  
- `--startup-project` → The API project that configures EF Core at runtime  
- `.` means “current directory” is the API project
---

# 2️⃣ Apply Migration to the Database  -Updates the actual SQL database using the latest migration files.
```bash
dotnet ef database update --project ../ExpenseTracker.Infrastructure --startup-project .
```
**What it does:**
- Applies *all pending migrations* to your running SQL Server container (or local SQL)
- Creates or updates tables
- Applies column changes, constraints, indexes, etc.
---

# 3️⃣ Remove Last Migration (Undo) -If you created the wrong migration or want to redo it:
```bash
dotnet ef migrations remove --project ../ExpenseTracker.Infrastructure --startup-project .
```
**Notes:**
- This removes only the latest migration file  
- If the migration was already applied to the DB, run:
```bash
dotnet ef database update LastGoodMigrationName
```
---

# 6️⃣ List All Migrations -Shows all migrations EF Core knows about:
```bash
dotnet ef migrations list --project ../ExpenseTracker.Infrastructure --startup-project .
```
---

# 7️⃣ Check the Database Connection (Design‑time) -EF Core sometimes needs to ensure the DbContext can be created.
```bash
dotnet ef dbcontext info --project ../ExpenseTracker.Infrastructure --startup-project .
```
This prints:
- Connection string  
- Provider (SQL Server)  
- Context type  
- Options  
---
