flowchart TD

    subgraph Client
        UI[React Frontend<br/>MSAL Auth]
    end

    subgraph API Layer
        API[ExpenseTracker.Api<br/>ASP.NET Core 8]
    end

    subgraph AppLayer[Application Layer]
        APP[Services<br/>DTOs<br/>Mapping<br/>Validation]
    end

    subgraph DomainLayer[Domain Layer]
        DOMAIN[Entities<br/>Interfaces<br/>BaseEntity]
    end

    subgraph InfraLayer[Infrastructure Layer]
        INFRA[EF Core<br/>DbContext<br/>Repositories<br/>SQL Integration]
    end

    subgraph Database
        SQL[(SQL Server<br/>Local or Azure SQL)]
    end

    UI -->|HTTP / JSON| API
    API --> APP
    API --> INFRA
    APP --> DOMAIN
    INFRA --> APP
    INFRA --> SQL