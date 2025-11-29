## Create the Solution
    dotnet new sln -n SolutionNameHere

## Create the API Project in its Own Folder
    dotnet new webapi -n ProjectNameHere.Api -o ProjectNameHere.Api

## Add the API Project to the Solution
    dotnet sln add ProjectNameHere.Api/ProjectNameHere.Api.csproj

## (Optional) Add a Sample Controller for Testing
    dotnet new apicontroller -n WeatherForecastController -o ProjectNameHere.Api/Controllers

## Add Swagger
    dotnet add package Swashbuckle.AspNetCore

## Add Swagger to Program.cs
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

## Run the API Locally
    dotnet run --project ProjectNameHere.Api

## Swagger UI will be available at:
    http://localhost:5000/swagger

## Create the Infrastructure Project in its Own Folder
    dotnet new classlib -n ProjectNameHere.Infrastructure -o ProjectNameHere.Infrastructure

## Add the Infrastructure Project to the Solution
    dotnet sln add ProjectNameHere.Infrastructure/ProjectNameHere.Infrastructure.csproj

## Create the Application Project in its Own Folder
    dotnet new classlib -n ProjectNameHere.Application -o ProjectNameHere.Application

## Add the Application Project to the Solution
    dotnet sln add ProjectNameHere.Application/ProjectNameHere.Application.csproj

## Create the Domain Project in its Own Folder
    dotnet new classlib -n ProjectNameHere.Domain -o ProjectNameHere.Domain

## Add the Domain Project to the Solution
    dotnet sln add ProjectNameHere.Domain/ProjectNameHere.Domain.csproj


## Add Reference from Application Layer to Domain Layer
    dotnet add ProjectNameHere.Application/ProjectNameHere.Application.csproj \
    reference ProjectNameHere.Domain/ProjectNameHere.Domain.csproj

## Add Reference from API Layer to Application Layer
    dotnet add ProjectNameHere.Api/ProjectNameHere.Api.csproj \
    reference ProjectNameHere.Application/ProjectNameHere.Application.csproj

## Add Reference from Infrastructure Layer to Application Layer
    dotnet add ProjectNameHere.Infrastructure/ProjectNameHere.Infrastructure.csproj \
    reference ProjectNameHere.Application/ProjectNameHere.Application.csproj

## Add Reference from API Layer to Infrastructure Layer
    dotnet add ProjectNameHere.Api/ProjectNameHere.Api.csproj \
    reference ProjectNameHere.Infrastructure/ProjectNameHere.Infrastructure.csproj