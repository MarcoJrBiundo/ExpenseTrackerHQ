

# Infrastructure Tests – Full Explanation

This document explains **every file** inside the Infrastructure.Tests project and how they work together.  
By the end, you should be able to re‑create the entire test setup from scratch.

---

# 1️⃣ SqliteExpensesDbContextFactory.cs

**Purpose:**  
Creates a fresh, isolated, in‑memory SQLite database for EACH test.

```csharp
var connection = new SqliteConnection("Filename=:memory:");
connection.Open();
```

- `Filename=:memory:` → SQLite runs entirely in RAM  
- `connection.Open()` → Must be opened early or SQLite won't persist tables between commands  

```csharp
var options = new DbContextOptionsBuilder<ExpenseDbContext>()
    .UseSqlite(connection)
    .Options;
```

- Builds EF Core options using the open SQLite connection  
- Points EF Core at an in‑memory SQL database  

```csharp
var context = new ExpenseDbContext(options);
context.Database.EnsureCreated();
```

- Creates a real EF Core `ExpenseDbContext`  
- `EnsureCreated()` builds the schema from your entity configurations  
- No migrations necessary for tests  

**Outcome:**  
Each test gets a clean database with real SQL behavior (queries, constraints, tracking).

---

# 2️⃣ ExpenseBuilder.cs

**Purpose:**   
A simple builder class used to create Expense entities for tests without repeating boilerplate.

### Why it exists
Writing this over and over:

```csharp
new Expense { UserId = ..., Amount = ..., Category = ... }
```

Would clutter tests.

### How it works
- Holds private fields for each property.
- Has fluent methods like `.WithAmount(...)` and `.WithUserId(...)`.
- `.Build()` returns a fully‑constructed Expense entity.

This improves **test readability** and **reduces duplication**.

---

# 3️⃣ ExpenseRepositoryTests.cs

This file contains all tests for the `ExpensesRepository`.

Below is a test‑by‑test explanation.

---

## ✅ Test 1: GetExpensesByUserAsync_returns_only_expenses_for_that_user

**Purpose:**  
Ensure the repository filters by `UserId` correctly and applies `AsNoTracking()`.

### What it does:
1. Creates a test DB
2. Creates:
   - Two expenses for user A  
   - One expense for user B  
3. Saves them to the DB  
4. Calls `GetExpensesByUserAsync(userA)`  
5. Asserts:
   - Only two records are returned  
   - Both belong to user A  
   - Tracking is disabled (ChangeTracker is empty)

This verifies:
- Correct filtering  
- Correct projection  
- Correct use of AsNoTracking  

---

## ✅ Test 2: GetExpenseByIdAsync_returns_expense_when_it_exists_for_user

**Purpose:**  
Verify the repository returns a specific expense IF the userId matches.

Steps:
1. Insert an expense for a given user  
2. Query it by userId + expenseId  
3. Assert the returned entity matches all expected fields  

This validates:
- Correct filtering by both UserId and ExpenseId  
- Correct handling of matching entity  

---

## ✅ Test 3: GetExpenseByIdAsync_returns_null_when_expense_does_not_exist

**Purpose:**  
Ensure no exceptions occur when the record doesn’t exist.

Steps:
1. No expense is inserted  
2. Query a random Guid  
3. Expected result → `null`

This proves:
- Method gracefully returns null  
- Does NOT throw  

---

## ✅ Test 4: GetExpenseByIdAsync_returns_null_when_expense_belongs_to_different_user

**Purpose:**  
Verify security filtering:  
A user CANNOT fetch an expense belonging to someone else.

Steps:
1. Insert an expense for user B  
2. Query with user A's userId  
3. Should return `null`

Ensures multi‑tenant safety.

---

## ✅ Test 5: AddExpenseAsync_assigns_new_id_when_empty_and_tracks_entity

**Purpose:**  
Validate ID generation and entity tracking before saving.

Steps:
1. Create an expense with `Id = Guid.Empty`  
2. Call repository.AddExpenseAsync  
3. Assert:
   - A new GUID is assigned  
   - The entity is in the Added state  
4. Call SaveChangesAsync  
5. Assert entity exists in DB  

This verifies:
- Repository should *not* save changes (Unit of Work responsibility)  
- EF Core tracking works as expected  
- ID assignment is correct  

---

## ✅ Test 6: AddExpenseAsync_throws_when_expense_is_null

**Purpose:**  
Test the guard clause.

Steps:
1. Pass `null` into `AddExpenseAsync`  
2. Expect `ArgumentNullException`

Ensures defensive programming is in place.

---

## ✅ Test 7: DeleteExpense_marks_entity_for_deletion_and_removes_from_database_after_save

**Purpose:**  
Verify correct deletion behavior.

Steps:
1. Insert an expense  
2. Call DeleteExpense  
3. Assert entity is marked as Deleted  
4. SaveChanges  
5. Assert entity is gone from DB  

This ensures:
- Repository deletes correctly  
- Unit of Work (SaveChanges) finalizes deletion  

---

# Flow Summary

Here’s the full lifecycle of your test environment:

### 1. Factory creates an isolated in-memory SQLite DB  
→ behaves like a real relational DB  
→ schema is created automatically  

### 2. Builder produces consistent Expense test objects  
→ avoids repetition  
→ keeps tests clean  

### 3. Tests verify:  
- Query filtering  
- AsNoTracking behavior  
- Entity state changes  
- Null handling  
- ID generation  
- Deletion behavior  

### 4. EF Core + SQLite ensures:  
- Real SQL behavior  
- Real constraints  
- Real tracking  
- Faster than SQL Server  

---

