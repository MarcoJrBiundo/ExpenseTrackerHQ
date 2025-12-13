using System;

namespace ExpenseTracker.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; protected set; } //Always stored in UTC
    public DateTime UpdatedAt { get; protected set; }//Always stored in UTC
    
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
