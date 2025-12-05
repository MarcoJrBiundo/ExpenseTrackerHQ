# Infrastructure Layer Setup

The Infrastructure layer contains all technology-specific implementations such as database access, EF Core, repositories, and Unit of Work.  
It is the only layer that interacts directly with external systems.

The purpose of Infrastructure is to implement interfaces defined in the Application layer — not to define business logic.

---

## What the Infrastructure Layer Contains

- DbContext
- All EF Core files and migrations
- Entity configurations using Fluent API
- Repository implementations
- Unit of Work implementation
- Dependency Injection wiring for all Infrastructure services

---

## Folder Structure

```
Infrastructure
│
├── Extensions/ - 
│     └── DependencyInjection.cs - Used to register Infrastructure services into the DI container.
│         - Registers DbContext
│         - Registers Repositories
│         - Registers UnitOfWork
│         - Reads connection string from appsettings
│
├── Persistence/
│     ├── Configurations/
│     │     └── EntityConfiguration.cs - Fluent API configuration for table/columns.  Relationships, keys, indexes, restrictions, etc.
│     │
│     ├── Migrations/ -All EF Core migration files live here
│     │
│     └── EntityDbContext.cs - The main database context, Applies entity configurations, Optionally implements:( audit fields, soft delete patterns, SaveChangesAsync overrides)

├── Repositories/
│     └── EntityRepository.cs - Implements IEntityRepository, Repository implementations that use EF Core internally.These do not contain business logic — only data access logic. CRUD
│
└── UnitOfWork/
      └── UnitOfWork.cs - Handles transaction-like behavior: One SaveChangesAsync call after repository operations, Ensures atomic consistency, Used in Command Handlers
```

---

## Required NuGet Packages
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.EntityFrameworkCore.Design`

## ServiceCollectionExtensions.cs Example
```c#
public static class ServiceCollectionExtensions
{
      public static void AddInfrastructure(this IServiceCollection services, IConfiguration   configuration)
      {
            var connectionString = configuration.GetConnectionString("EntityTrackerDb")?? throw new InvalidOperationException("Connection string 'EntityTrackerDb' not found.");
            services.AddDbContext<EntityDbContext>(options => { options.UseSqlServer(connectionString);});
            services.AddScoped<IEntityRepository, EntityRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
      }
}   
```
## EntityConfiguration.cs Example
```c# 
public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
      builder.ToTable("Entity");
      builder.HasKey(e => e.Id);
      builder.Property( e => e.Id).ValueGeneratedOnAdd();
      builder.Property(e => e.UserId).IsRequired();
      builder.Property(e => e.Amount).HasColumnType("decimal(18,2)") .IsRequired();
      builder.Property(e => e.Description) .HasMaxLength(500);
      builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()").IsRequired();
      builder.Property(e => e.UpdatedAt) .HasDefaultValueSql("GETUTCDATE()").IsRequired();
    }
}
```


## EntityDbContext.cs Example

```c#
public sealed class EntityDbContext(DbContextOptions<EntityDbContext> options) : DbContext(options)
{
      public DbSet<Entity> Entites { get; set; } = default!;
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntityConfiguration).Assembly);
            base.OnModelCreating(modelBuilder);
      }   


      public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
      {
            var utcNow = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
            if (entry.State == EntityState.Added)
            {
                  entry.Entity.SetCreated(utcNow);
                  entry.Entity.SetUpdated(utcNow);
            }
            else if (entry.State == EntityState.Modified)
                  entry.Entity.SetUpdated(utcNow);
            }
            return base.SaveChangesAsync(cancellationToken);
      }
}
```

## UnitOfWork.cs Example
```c#
public class UnitOfWork : IUnitOfWork
{
    private readonly EntityDbContext _dbContext;

    public UnitOfWork(EntityDbContext dbContext) { _dbContext = dbContext; }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```


## EntityRepo.cs Example
```c#

public class EntityRepository : IEntityRepository
{
      private readonly EntityDbContext _dbContext;

      public EntityRepository(EEntityDbContext dbContext) { _dbContext = dbContext; }

      public async Task<Guid> AddEntityAsync(Entity entity, CancellationToken cancellationToken = default)
      {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

            await _dbContext.Entitys.AddAsync(entity, cancellationToken);
            return entity.Id;
      }

      public Task DeleteEntity(Entity entity, CancellationToken cancellationToken = default)
      {
            _dbContext.Entitys.Remove(entity);
            return Task.CompletedTask;
      }

      public Task<Entity?> GetEntityByIdAsync(Guid userId, Guid entityId, CancellationToken cancellationToken = default)
      {
            return _dbContext.Entitys.FirstOrDefaultAsync(e => e.Id == entityId && e.UserId == userId, cancellationToken);
      }

      public async Task<IEnumerable<Entity>> GetEntitysByUserAsync(Guid userId, CancellationToken cancellationToken = default)
      {
            IReadOnlyList<Entity> entity = await _dbContext.Entitys.Where(e => e.UserId == userId).AsNoTracking().ToListAsync(cancellationToken);
            return entity; 
      }

      public async Task<IReadOnlyList<Entity>> GetEntitysByUserViaStoredProcAsync( Guid userId, CancellationToken cancellationToken = default)
      {
            var userIdParam = new SqlParameter("@UserId", userId);

            var query = _dbContext.Entitys
            .FromSqlRaw("EXEC [dbo].[sp_GetEntitysByUser] @UserId", userIdParam)
            .AsNoTracking();

            return await query.ToListAsync(cancellationToken);
      }
}

```


```c#

```