## Diagram 1 — Architecture Overview (what exists + main flows)

```mermaid
flowchart LR
  U["User / Developer<br/>Browser or Postman"]
  APIM["Azure API Management<br/>Public Gateway"]
  AKS["AKS Cluster<br/>ExpenseTracker API"]
  KV["Azure Key Vault<br/>RBAC"]
  SQL["Azure SQL Database (PaaS)\nPrivate access only"]
  ACR["Azure Container Registry"]
  PE["Private Endpoint (NIC)"]

  U -->|HTTPS| APIM
  APIM -->|Routes to backend| AKS
  AKS -->|Pull images| ACR
  AKS -->|Get secrets| KV
  AKS -->|TCP 1433| PE
  PE -->|Private link| SQL

  subgraph AZ["Azure Subscription"]
    APIM
    ACR
    KV
    subgraph VNET["Virtual Network"]
      subgraph AKS_SYSTEM_SUBNET["AKS System Node Subnet"]
        SYS["System Node Pool\n(kube-system)"]
      end
      subgraph AKS_SUBNET["AKS User Node Subnet"]
        AKS
      end
      subgraph PE_SUBNET["Private Endpoint Subnet"]
        PE
      end
      SQL
    end
  end
```

---

## Diagram 2 — Request Authentication (user → Entra → APIM → API)

```mermaid
flowchart LR
  U[User]
  ENTRA[Entra External ID]
  APIM[API Management]
  API[ExpenseTracker API]

  U -->|OIDC sign-in| ENTRA;
  ENTRA -->|Access token| U;
  U -->|Bearer token| APIM;
  APIM -->|Validate JWT| ENTRA;
  APIM -->|Forward request| API;
```

---

## Diagram 3 — Workload Identity + Key Vault (API/Migrations → secrets)

```mermaid
flowchart LR
  subgraph AKS[AKS Cluster]
    SA[K8s ServiceAccount]
    API[API Pods]
    MIG[Migrations Job]
    OIDC[AKS OIDC Issuer]

    SA --> API;
    SA --> MIG;
    OIDC --> API;
    OIDC --> MIG;
  end

  ENTRA[Entra ID]
  KV[Key Vault]

  API -->|OIDC federation| ENTRA;
  MIG -->|OIDC federation| ENTRA;
  API -->|Get secrets| KV;
  MIG -->|Get secrets| KV;
```

---

## Diagram 4 — Private Networking (AKS → SQL via Private Endpoint + Private DNS)

```mermaid
flowchart LR
  subgraph VNET["Virtual Network"]
    DNS["Private DNS Zone<br/>privatelink.database.windows.net"]
    subgraph AKS_SUBNET["AKS Subnet"]
      AKS["AKS Pods"]
    end
    subgraph PE_SUBNET["Private Endpoint Subnet"]
      PE["Private Endpoint (NIC)"]
    end
  end

  AKS -->|Resolve SQL FQDN| DNS
  DNS -->|Returns private IP| AKS

  SQL["Azure SQL Server/DB"] --- PE

  AKS -->|TCP 1433| PE
  PE -->|Private link| SQL
```