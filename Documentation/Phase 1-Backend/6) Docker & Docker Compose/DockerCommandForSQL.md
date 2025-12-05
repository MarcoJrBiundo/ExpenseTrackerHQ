## Docker Commands

This file documents the key Docker commands used (or typically needed) to:
Container name used below: `entity-sql`
Image used: `mcr.microsoft.com/mssql/server:2022-latest`


## 1️⃣ Create & Run the SQL Server Container
This is the main command to spin up a local SQL Server instance in Docker:
```bash
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD="Your_password123\!" -p 1433:1433 --name entity-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

**What this does:**
- `-e ACCEPT_EULA=Y` → Accepts the SQL Server license agreement
- `-e SA_PASSWORD=...` → Sets the `sa` user password
- `-p 1433:1433` → Maps container port 1433 to local port 1433 (so your app/SSMS can connect)
- `--name entity-sql` → Names the container for easy reference
- `-d` → Runs the container in detached (background) mode
- `mcr.microsoft.com/mssql/server:2022-latest` → Image to use (SQL Server 2022)
After this runs successfully, SQL Server is running inside the `entity-sql` container.

---



## 5️⃣ Connect to SQL Server Using `sqlcmd` (Inside the Container)
If `sqlcmd` is installed in the image (it usually is on the official SQL Server Linux image), you can run it from inside the container:
```bash
docker exec -it entity-sql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Your_password123\!"
```
- `-S localhost` → Connects to SQL Server inside the container
- `-U sa` / `-P ...` → Uses the `sa` account and the password you defined

From there you can run queries, create databases, etc.

Example inside `sqlcmd`:

```sql
CREATE DATABASE ExpenseTrackerDb;
GO
```
---
