USE [ExpenseTrackerDb];
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetExpensesByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.Id,
        e.UserId,
        e.Amount,
        e.Category,
        e.Description,
        e.Currency,
        e.CreatedAt,
        e.UpdatedAt,
        e.Date
    FROM dbo.Expenses e
    WHERE e.UserId = @UserId
    ORDER BY e.Date
END
GO