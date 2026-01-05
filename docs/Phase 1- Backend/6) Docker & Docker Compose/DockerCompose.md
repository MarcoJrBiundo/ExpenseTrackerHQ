
# Docker Compose – ExpenseTracker Api + SQL Server

This document explains the `docker-compose.yml` file used to run the **ExpenseTracker API** and a **SQL Server** database together using Docker Compose.

The goal is:
- One command to start both **API** and **database**
- Consistent configuration
- Easy to tear down and recreate

Below is the current `docker-compose.yml` for reference:

```yaml
version: "3.9"

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: expense-sql
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrongPassword1!
    ports:
      - "1433:1433"
    volumes:
      - expense-sqldata:/var/opt/mssql
    restart: unless-stopped

  api:
    container_name: expense-api
    build:
      context: .
      dockerfile: ExpenseTracker.Api/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__ExpenseTrackerDb=Server=expense-sql,1433;Database=ExpenseTrackerDb;User Id=sa;Password=YourStrongPassword1!;TrustServerCertificate=True;
    ports:
      - "4200:8080"
    depends_on:
      - sqlserver
    restart: unless-stopped

volumes:
  expense-sqldata:
```

---

## 1️⃣ Top-Level Version

```yaml
version: "3.9"
```

- This specifies the **Compose file format version**.
- Version `3.9` is compatible with recent Docker and Docker Compose.
- It does **not** affect your app logic; it just tells Docker which schema to use when reading this file.

If you ever recreate this file, you can safely use:

```yaml
version: "3.9"
```

or omit it in newer Compose versions (v2+), but keeping it is fine and explicit.

---

## 2️⃣ `services` Section

All running containers are defined under `services:`.  
Each service is a container (or group of containers) that makes up your application.

In this file, you have two services:

- `sqlserver` → The **database** (SQL Server 2022 on Linux)
- `api` → The **ExpenseTracker.Api** backend

```yaml
services:
  sqlserver:
    ...

  api:
    ...
```

Each service has its own configuration: image, container name, ports, env vars, dependencies, etc.

---

## 3️⃣ SQL Server Service (`sqlserver`)

```yaml
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: expense-sql
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrongPassword1!
    ports:
      - "1433:1433"
    volumes:
      - expense-sqldata:/var/opt/mssql
    restart: unless-stopped
```

### `image`

```yaml
image: mcr.microsoft.com/mssql/server:2022-latest
```

- Tells Docker which **image** to run.
- Here I use the official **SQL Server 2022** Linux image from Microsoft.
- `:2022-latest` is the tag for that version.

If you changed versions in the future, this is where you’d point to a different SQL Server image or tag.

### `container_name`

```yaml
container_name: expense-sql
```

- This sets a fixed Docker container name instead of a random one.
- Other services (like the API) can refer to this service using the **service name** `sqlserver`, but this `container_name` is helpful for:
  - Running `docker logs expense-sql`
  - Running `docker exec -it expense-sql /bin/bash`

You could change the name later if you like, but then you’d also update any docs that reference it.

### `environment`

```yaml
environment:
  - ACCEPT_EULA=Y
  - SA_PASSWORD=YourStrongPassword1!
```

- `ACCEPT_EULA=Y`
  - Required by Microsoft to accept the SQL Server license agreement.
- `SA_PASSWORD=YourStrongPassword1!`
  - Sets the password for the `sa` (system administrator) SQL Server account.
  - Must meet SQL Server password complexity rules.

In a real production setup, this password would not be committed to source control — you’d use secrets or environment files. For local dev, this is acceptable, but still something to keep in mind.

### `ports`

```yaml
ports:
  - "1433:1433"
```

- Maps **host port 1433** → **container port 1433**.
- This allows **tools on your machine** (SSMS, Azure Data Studio, Rider, etc.) to connect using:
  - `localhost,1433`
  - or `127.0.0.1,1433`

The API also connects to SQL Server **inside the Docker network** using the hostname `expense-sql` (or `sqlserver`) on port `1433`.

### `volumes`

```yaml
volumes:
  - expense-sqldata:/var/opt/mssql
```

- Mounts a **named Docker volume** called `expense-sqldata` at `/var/opt/mssql` inside the container.
- `/var/opt/mssql` is where SQL Server stores its data files.
- This ensures that your database **persists** even if the container is removed.

If you delete the container but keep the volume, your data remains. If you delete the volume too, you lose all data.

### `restart`

```yaml
restart: unless-stopped
```

- Instructs Docker to restart the container automatically:
  - It will restart if Docker daemon restarts or the container crashes.
  - It will **not** restart if you explicitly stop it (`docker stop`).

---

## 4️⃣ API Service (`api`)

```yaml
  api:
    container_name: expense-api
    build:
      context: .
      dockerfile: ExpenseTracker.Api/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__ExpenseTrackerDb=Server=expense-sql,1433;Database=ExpenseTrackerDb;User Id=sa;Password=YourStrongPassword1!;TrustServerCertificate=True;
    ports:
      - "4200:8080"
    depends_on:
      - sqlserver
    restart: unless-stopped
```

### `container_name`

```yaml
container_name: expense-api
```

- Sets a fixed name for the API container.
- Makes logs and exec commands easier, like:
  - `docker logs expense-api`
  - `docker exec -it expense-api /bin/bash`

### `build`

```yaml
build:
  context: .
  dockerfile: ExpenseTracker.Api/Dockerfile
```

This tells Docker **how to build the image** for the API:

- `context: .`
  - The build context is the current folder (same folder where `docker-compose.yml` lives).
  - Docker will send this directory (recursively) to the Docker daemon as the build context.

- `dockerfile: ExpenseTracker.Api/Dockerfile`
  - Relative path to the Dockerfile from the build context.
  - In your case, the Dockerfile for the API lives at `./ExpenseTracker.Api/Dockerfile`.

If you move the Dockerfile later, you must update this path.

### `environment`

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ConnectionStrings__ExpenseTrackerDb=Server=expense-sql,1433;Database=ExpenseTrackerDb;User Id=sa;Password=YourStrongPassword1!;TrustServerCertificate=True;
```

- `ASPNETCORE_ENVIRONMENT=Development`
  - Sets the ASP.NET Core environment.
  - Your app can load **appsettings.Development.json** and use dev-specific configuration.

- `ConnectionStrings__ExpenseTrackerDb=...`
  - Uses **ASP.NET Core configuration binding** with double underscores (`__`) to represent nested config.
  - This maps to:
    - `Configuration["ConnectionStrings:ExpenseTrackerDb"]`
  - The value is a standard SQL Server connection string:

    ```text
    Server=expense-sql,1433;Database=ExpenseTrackerDb;User Id=sa;Password=YourStrongPassword1!;TrustServerCertificate=True;
    ```

  - `Server=expense-sql,1433` → refers to the **container name** / service name `expense-sql` inside the Docker network, on port 1433.
  - `Database=ExpenseTrackerDb` → DB name your EF Core migrations will create.
  - `User Id=sa;Password=...` → Credentials set in the `sqlserver` service.
  - `TrustServerCertificate=True;` → Skips strict certificate validation for local dev.

### `ports`

```yaml
ports:
  - "4200:8080"
```

- Maps **host port 4200** → **container port 8080**.
- Inside the container, Kestrel is listening on `http://*:8080` (configured in your Dockerfile / app).
- On your machine, you hit:
  - `http://localhost:4200/swagger`
  - or `http://localhost:4200/api/...`

If you ever change the port Kestrel listens on inside the container, you must update the **right-hand side** (e.g. `4200:8081`).

If you want to expose a different external port (e.g. 5000), update the **left-hand side** (e.g. `5000:8080`).

### `depends_on`

```yaml
depends_on:
  - sqlserver
```

- Tells Docker Compose: **start `sqlserver` before `api`.**
- This does **not** guarantee SQL Server is fully ready, but it guarantees the container is started.
- EF Core connection retries (if you add them) help handle cases where SQL is still starting.

### `restart`

```yaml
restart: unless-stopped
```

Same behavior as the SQL Server container:
- Automatically restarts if Docker restarts or container crashes.
- Does not restart if you explicitly stop it.

---

## 5️⃣ Volumes Section

```yaml
volumes:
  expense-sqldata:
```

This defines a named volume, `expense-sqldata`.

- It is referenced in the `sqlserver` service:

  ```yaml
  volumes:
    - expense-sqldata:/var/opt/mssql
  ```

- Defined here at the bottom so Docker knows it is a **named volume** rather than an anonymous one.
- It persists SQL Server data beyond the lifecycle of the container.

If you ever want to completely reset all DB data, you can remove this volume:

```bash
docker volume rm expense-sqldata
```

But be careful: that will delete all data.

---

## 6️⃣ How to Use This Compose File

From the folder containing `docker-compose.yml`:

### Start everything (build if needed)

```bash
docker compose up -d
```

- Builds the `api` image (if not built already)
- Starts `sqlserver` and `api` containers in the background (`-d`)

### Rebuild the API after code changes in Dockerfile or project

```bash
docker compose build api
```

or

```bash
docker compose up -d --build
```

### Stop containers (but keep them and the volume)

```bash
docker compose down
```

### Stop and also remove the named volume (wipe database data)

```bash
docker compose down -v
```

---

## 7️⃣ How to Recreate This File From Scratch (Mental Model)

If you ever delete this file and need to rebuild it:

1. Start with:
   ```yaml
   version: "3.9"
   services:
   ```

2. Add a `sqlserver` service:
   - Use `mcr.microsoft.com/mssql/server:2022-latest`
   - Set `ACCEPT_EULA` and `SA_PASSWORD`
   - Map `1433:1433`
   - Mount `expense-sqldata:/var/opt/mssql`

3. Add an `api` service:
   - Use `build:` with `context: .` and the relative path to your Dockerfile
   - Set `ASPNETCORE_ENVIRONMENT`
   - Add the `ConnectionStrings__...` environment variable with the `Server=expense-sql,1433;...` connection string
   - Map `4200:8080`
   - Add `depends_on: - sqlserver`

4. At the bottom, declare:
   ```yaml
   volumes:
     expense-sqldata:
   ```

If you remember those building blocks, you can always reconstruct the entire file.

---

## Summary

This `docker-compose.yml`:

- Spins up **SQL Server** with a persistent data volume
- Builds and runs the **ExpenseTracker API** in another container
- Connects the API to SQL Server using Docker's internal network
- Exposes the API on `http://localhost:4200` and SQL Server on `localhost:1433`
- Provides a fully self-contained local dev environment using Docker Compose.
