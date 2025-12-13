# ================================
# STAGE 1: build & publish
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# 1. Set working directory inside the container
WORKDIR /src

# 2. Copy csproj files first (leverages Docker layer caching for faster builds)
COPY ["ExpenseTracker.Api/ExpenseTracker.Api.csproj", "ExpenseTracker.Api/"]
COPY ["ExpenseTracker.Application/ExpenseTracker.Application.csproj", "ExpenseTracker.Application/"]
COPY ["ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj", "ExpenseTracker.Infrastructure/"]
COPY ["ExpenseTracker.Domain/ExpenseTracker.Domain.csproj", "ExpenseTracker.Domain/"]

# 3. Restore dependencies
RUN dotnet restore "ExpenseTracker.Api/ExpenseTracker.Api.csproj"

# 4. Copy the rest of the source code
COPY . .

# 5. Move into the API project folder
WORKDIR /src/ExpenseTracker.Api

# 6. Publish the app (Release, self-contained trimmed app disabled, generate only DLL)
RUN dotnet publish "ExpenseTracker.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false


# ================================
# STAGE 2: runtime image
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# 7. Set working directory for the runtime container
WORKDIR /app

# 8. Copy published output from build stage
COPY --from=build /app/publish .

# 9. Configure port inside container (we'll map it to a host port when running)
ENV ASPNETCORE_URLS=http://+:8080

# 10. Optional: set environment (can override with -e at runtime)
ENV ASPNETCORE_ENVIRONMENT=Development

# 11. Expose the port (for documentation only; docker run -p is what really matters)
EXPOSE 8080

# 12. Entrypoint: run the API
ENTRYPOINT ["dotnet", "ExpenseTracker.Api.dll"]