## Diagram 1 — Architecture Overview (what exists + main flows)



**30‑Second Walkthrough**

This diagram shows the full platform at a glance. All traffic enters through Azure API Management, which routes requests to the ExpenseTracker API running in AKS. The cluster pulls images from Azure Container Registry and retrieves secrets from Key Vault at runtime. AKS runs inside a virtual network with separate subnets for system nodes, application workloads, and private endpoints. Azure SQL is a PaaS service with no public access — the API connects to it only through a private endpoint over the Azure backbone.

---

## Diagram 2 — Request Authentication (user → Entra → APIM → API)



**30‑Second Walkthrough**

This diagram focuses on user authentication. A user signs in with Microsoft Entra External ID and receives a JWT access token. That token is sent to API Management with each request. API Management validates the token’s issuer, audience, and scopes before forwarding the request to the backend API. If the token is invalid, the request never reaches the application.

---

## Diagram 3 — Workload Identity + Key Vault (API/Migrations → secrets)


**30‑Second Walkthrough**

This diagram shows how the application accesses secrets without credentials. API pods and the database migration job run with a Kubernetes service account using workload identity. At runtime, they exchange that identity with Entra ID via OIDC federation. Once authenticated, they retrieve secrets from Azure Key Vault using RBAC. No secrets are stored in code, Helm, or environment variables.

---

## Diagram 4 — Private Networking (AKS → SQL via Private Endpoint + Private DNS)



**30‑Second Walkthrough**

This diagram explains private database connectivity. When the API resolves the SQL hostname, Private DNS returns a private IP instead of a public one. Traffic flows from AKS to a private endpoint inside the virtual network and then to Azure SQL over TCP 1433. The database has no public access — all connectivity stays on the Azure backbone.