# Sprint 4.2 — Secure Database Connectivity (THE BIBLE)

> **Purpose of this document**
>
> This is the “holy bible” for how I connected **Azure SQL** to our **AKS pods** **securely** and **privately** in Sprint 4.2.
>
> It is intentionally **excruciatingly detailed**.
>
> If you follow this document from top to bottom on a fresh environment, you should be able to reproduce:
> - ✅ AKS → Azure SQL connectivity over **Private Endpoint + Private DNS**
> - ✅ **No DB secrets** in code / Git / Helm values / Kubernetes manifests
> - ✅ Runtime secret retrieval via **Azure Key Vault**
> - ✅ Authentication via **AKS Workload Identity (OIDC)**
> - ✅ Database schema creation via **EF Core migrations runner** executed as a **Helm hook Kubernetes Job**
>
> This includes the exact problems I hit (placeholder overrides, DNS checks, container framework mismatch, tag caching) and how I fixed them.

---

## Sprint Goal

Wire the API to Azure SQL **securely and privately**, using:

- **Azure SQL Private Endpoint** (provisioned in Phase 3)
- **Private DNS** for `privatelink.database.windows.net` name resolution (Phase 3)
- **Azure Key Vault** as the source of truth for secrets
- **AKS Workload Identity (OIDC)** so pods can access Key Vault **without any credential stored anywhere**

By the end of this sprint:

- ✅ API in AKS can **read/write** to Azure SQL.
- ✅ DB credentials are stored **only** in Key Vault.
- ✅ No DB secrets exist in:
  - Git repo
  - Helm values
  - Kubernetes manifests
  - Terraform state
  - environment variables
- ✅ EF schema is applied automatically using a **migration runner job** (production-safe pattern).

---

## Mental Model (Read This First)

### What I are trying to achieve

I want the API pod to connect to Azure SQL, but I want:

1) **Network**: traffic flows privately
- The hostname `sql-<name>.database.windows.net` must resolve to a **private IP** (Private DNS)
- TCP traffic goes to SQL over **private endpoint** (no public internet)

2) **Secret**: DB username/password are never stored in app settings / Helm / manifests
- The connection string lives in **Key Vault**

3) **Identity**: pod gets permission to read the secret without storing any “Key Vault password”
- Pod uses **Workload Identity (OIDC)** → Entra ID issues token → Key Vault allows read via RBAC

4) **Schema**: tables must exist
- EF migrations are applied via a separate process (Kubernetes Job) before the API runs

### Why I can’t just put the connection string in Helm values
Because Helm values end up in:
- Git (if committed)
- CI logs
- `helm get values`
- Kubernetes objects

Even “dev” should practice the correct pattern.

---

## Prerequisites (Phase 3 outputs you must already have)

### A) Azure SQL is reachable privately

You must already have:

- Azure SQL Server + Database
- Private Endpoint for SQL (in a VNet subnet)
- Private DNS zone for SQL:
  - `privatelink.database.windows.net`
- A VNet link between that Private DNS zone and the VNet where AKS is deployed

**If any of these are missing, Sprint 4.2 will fail.**

### B) Sprint 4.1 completed

You must already have:

- Key Vault created
- Key Vault RBAC enabled
- Workload Identity enabled on AKS (OIDC)
- ServiceAccount annotated with the client ID of the user-assigned managed identity
- Federated Credential created for the ServiceAccount subject

**If Sprint 4.1 isn’t done, pods cannot read Key Vault.**

---

## Task 229 — SQL Secret Strategy in Key Vault

### Goal

Store the SQL credentials in **Key Vault**, and only ever retrieve them **at runtime**.

### Decision: “full connection string” vs “parts”

I chose:

✅ **Single secret containing the full SQL connection string**.

Why:
- Works with .NET’s built-in `ConnectionStrings:<name>` convention
- Fewer moving pieces
- Easier to rotate (change one secret value)

I can evolve later if I want per-component secrets.

### Key Vault secret details

| Property | Value |
|---|---|
| Key Vault | `expensetracker-dev-kv` |
| Primary Secret Name | `sql--expensetracker--connectionstring` |
| Secondary Secret Name (for .NET binding) | `ConnectionStrings--ExpenseTrackerDb` |
| Access model | Azure RBAC |
| Role | `Key Vault Secrets User` |

> **Important**: The `ConnectionStrings--ExpenseTrackerDb` secret name is intentional.
> The double-dash `--` becomes a colon `:` in .NET configuration binding.
>
> `ConnectionStrings--ExpenseTrackerDb` → `ConnectionStrings:ExpenseTrackerDb`

### Connection string template (DO NOT COMMIT REAL VALUE)

```text
Server=tcp:<SQL_SERVER_FQDN>,1433;Database=<DB_NAME>;User ID=<SQL_USER>;Password=<SQL_PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Notes:
- Always use `*.database.windows.net` FQDN.
- Private DNS makes it resolve to the private endpoint IP from AKS.
- `Encrypt=True` is required by Azure SQL.

### Commands: set secrets in Key Vault

> Use Azure CLI. Do not paste the real password into docs.

1) Create the main “human-named” secret:

```bash
az keyvault secret set \
  --vault-name expensetracker-dev-kv \
  --name "sql--expensetracker--connectionstring" \
  --value "Server=tcp:sql-<server>.database.windows.net,1433;Database=<db>;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

2) Create the .NET-friendly secret name (the one the app actually binds to):

```bash
az keyvault secret set \
  --vault-name expensetracker-dev-kv \
  --name "ConnectionStrings--ExpenseTrackerDb" \
  --value "$(az keyvault secret show --vault-name expensetracker-dev-kv --name 'sql--expensetracker--connectionstring' --query value -o tsv)"
```

This “copies” the value so you only maintain one canonical secret.

---

## Runtime: How the API Reads the Secret

### Configuration precedence (why overrides happen)

.NET reads configuration sources in an order. The usual effective precedence is:

1. Environment variables
2. Key Vault (if added)
3. appsettings.json

So if you accidentally set an env var like:

- `ConnectionStrings__ExpenseTrackerDb=Server=placeholder...`

…it will override Key Vault even if Key Vault is configured correctly.

This happened to us.

### Required .NET packages in the API

The API must reference:

- `Azure.Identity`
- `Azure.Security.KeyVault.Secrets`
- `Azure.Extensions.AspNetCore.Configuration.Secrets`

### Required API startup wiring (concept)

In `Program.cs` (or wherever you build configuration), you must:

1) Read `KeyVault__Uri`
2) Add Key Vault as a config source using `DefaultAzureCredential`

The key idea:

- `DefaultAzureCredential()` inside AKS uses Workload Identity
- The pod authenticates to Entra ID without storing secrets

---

## Helm Wiring (the exact rules)

### Rule 1: No DB creds in Helm values

I do **not** store:
- username
- password
- full connection string

in `values*.yaml`.

### Rule 2: Provide ONLY non-secret config via Helm

Examples of allowed values:
- Key Vault URI (not a secret)
- client ID (not a secret)
- labels, annotations

### Rule 3: ServiceAccount must be used by the Deployment

If the Deployment uses the `default` ServiceAccount, Workload Identity fails.

Fix I applied:

- Ensure deployment sets `serviceAccountName: expense-api`

### Rule 4: Pod label required for workload identity

Pods must have:

```yaml
podLabels:
  azure.workload.identity/use: "true"
```

Without this label, token exchange won’t happen.

---

## The Problems I Hit (and the exact fixes)

### Problem A — “Server not found / not accessible” (HTTP 500)

I saw:

```json
"detail": "A network-related or instance-specific error occurred ... TCP Provider ..."
```

This can mean:
- DNS not resolving to private endpoint
- SQL host wrong
- route/NSG issue

#### Debug step: DNS resolution from inside AKS

Run a temporary pod:

```bash
kubectl run -n expense-dev dns-check --rm -it --image=busybox:1.36 --restart=Never -- sh
```

Inside:

```sh
nslookup sql-expensetracker-dev-mb1319.database.windows.net
```

✅ Good output looks like:
- resolves to `*.privatelink.database.windows.net`
- returns a private IP like `10.x.x.x`

This confirmed our Private DNS + Private Endpoint path was correct.

### Problem B — Placeholder DB connection string overriding Key Vault

I found the pod had:

- `ConnectionStrings__ExpenseTrackerDb=Server=placeholder;Database=ExpenseTrackerDb;...`

That caused the app to attempt DNS lookup for `placeholder` which fails.

#### Why it happened

Our Helm chart created a Secret (`expense-api-secrets`) containing a placeholder connection string.
The Deployment used `envFrom` → the env var was always present.

#### Fix (the exact actions)

1) Remove the hardcoded placeholder from the Helm secret template.

I changed `infra/helm/expense-api/templates/secret.yaml` to:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: expense-api-secrets
  labels:
    {{- include "expense-api.labels" . | nindent 4 }}
type: Opaque
stringData:
  {{- with .Values.secrets }}
  {{- toYaml . | nindent 2 }}
  {{- end }}
```

Meaning: if I don’t set `.Values.secrets`, no secret env vars are injected.

2) Delete the already-created Kubernetes Secret so the stale key is gone:

```bash
kubectl delete secret -n expense-dev expense-api-secrets
```

3) Redeploy Helm so the Secret is recreated without the placeholder:

```bash
helm upgrade --install expense-api infra/helm/expense-api -n expense-dev -f infra/helm/expense-api/values-expense-dev.yaml
```

4) Restart deployment so pods re-read env vars:

```bash
kubectl rollout restart deployment/expense-api -n expense-dev
kubectl rollout status deployment/expense-api -n expense-dev
```

5) Verify the env var is gone:

```bash
kubectl exec -n expense-dev deploy/expense-api -c expense-api -- env | grep ConnectionStrings__ExpenseTrackerDb
```

Expected: **no output**.

### Proof I were now correctly connected

After removing the placeholder override, our error changed to:

```json
"detail": "Invalid object name 'Expenses'."
```

That is a GOOD sign.

It means:
- ✅ SQL connection succeeded
- ✅ auth succeeded
- ✅ network succeeded
- ❌ tables don’t exist yet

That is exactly what I expect before running migrations.

---

## US-344 — Database Migrations Strategy (Production-Safe)

### Goal

Apply EF Core migrations automatically in AKS, safely, and repeatably.

### Pattern I used

✅ **Dedicated migrations runner container** executed as a **Kubernetes Job** via a **Helm hook**:

- Hook runs on `pre-install` and `pre-upgrade`
- Job uses the same Workload Identity ServiceAccount
- Job reads connection string from Key Vault at runtime
- Job runs `db.Database.MigrateAsync()` and exits

This avoids the “migrations on API startup” anti-pattern.

---

## Step-by-step: Build the Migration Runner

### Step 1 — Create the console project

Project path:

`apps/migrations/ExpenseTracker.Migrations`

Commands:

```bash
cd apps
mkdir -p migrations
cd migrations

dotnet new console -n ExpenseTracker.Migrations
```

### Step 2 — Add it to the solution

Our solution file lives here:

`apps/api/ExpenseTracker.sln`

So add using:

```bash
dotnet sln apps/api/ExpenseTracker.sln add apps/migrations/ExpenseTracker.Migrations/ExpenseTracker.Migrations.csproj
```

### Step 3 — Reference infrastructure (for the DbContext)

```bash
cd apps/migrations/ExpenseTracker.Migrations

dotnet add reference ../../api/ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj
dotnet add reference ../../api/ExpenseTracker.Application/ExpenseTracker.Application.csproj
```

### Step 4 — Add required packages

```bash
cd apps/migrations/ExpenseTracker.Migrations

dotnet add package Azure.Identity
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets

dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### Step 5 — Implement Program.cs

File:

`apps/migrations/ExpenseTracker.Migrations/Program.cs`

This is the exact logic:
- Read `KeyVault__Uri` env var
- Use `DefaultAzureCredential()` (Workload Identity)
- Add Key Vault config provider
- Read `ConnectionStrings:ExpenseTrackerDb`
- Create `ExpenseDbContext`
- Apply migrations

```csharp
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== ExpenseTracker.Migrations starting ===");

try
{
    var builder = new ConfigurationBuilder()
        .AddEnvironmentVariables();

    var preConfig = builder.Build();

    var keyVaultUri = preConfig["KeyVault:Uri"]; // KeyVault__Uri -> KeyVault:Uri
    if (string.IsNullOrWhiteSpace(keyVaultUri))
    {
        Console.WriteLine("KeyVault:Uri missing. Set KeyVault__Uri. Exiting.");
        return 2;
    }

    Console.WriteLine($"Using Key Vault: {keyVaultUri}");

    var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
    builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

    var config = builder.Build();

    var conn = config.GetConnectionString("ExpenseTrackerDb");
    if (string.IsNullOrWhiteSpace(conn))
    {
        Console.WriteLine("ConnectionStrings:ExpenseTrackerDb missing/empty. Exiting.");
        return 3;
    }

    var options = new DbContextOptionsBuilder<ExpenseDbContext>()
        .UseSqlServer(conn)
        .Options;

    await using var db = new ExpenseDbContext(options);

    Console.WriteLine("Applying EF Core migrations...");
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations applied ✅");

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("Migration runner failed ❌");
    Console.WriteLine(ex);
    return 1;
}
finally
{
    Console.WriteLine("=== ExpenseTracker.Migrations finished ===");
}
```

Build it:

```bash
dotnet build apps/migrations/ExpenseTracker.Migrations/ExpenseTracker.Migrations.csproj
```

---

## Containerize the Migration Runner

### Dockerfile location

`apps/migrations/ExpenseTracker.Migrations/Dockerfile`

### Why I changed the base image

I initially used `mcr.microsoft.com/dotnet/runtime:8.0` and got:

> `Framework 'Microsoft.AspNetCore.App', version '8.0.0' was not found.`

Because `Azure.Extensions.AspNetCore.Configuration.Secrets` pulls in the ASP.NET shared framework.

✅ Fix: Use `mcr.microsoft.com/dotnet/aspnet:8.0` for runtime.

Dockerfile:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["apps/migrations/ExpenseTracker.Migrations/ExpenseTracker.Migrations.csproj", "apps/migrations/ExpenseTracker.Migrations/"]
COPY ["apps/api/ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj", "apps/api/ExpenseTracker.Infrastructure/"]
COPY ["apps/api/ExpenseTracker.Application/ExpenseTracker.Application.csproj", "apps/api/ExpenseTracker.Application/"]
COPY ["apps/api/ExpenseTracker.Domain/ExpenseTracker.Domain.csproj", "apps/api/ExpenseTracker.Domain/"]

RUN dotnet restore "apps/migrations/ExpenseTracker.Migrations/ExpenseTracker.Migrations.csproj"

COPY . .

WORKDIR "/src/apps/migrations/ExpenseTracker.Migrations"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExpenseTracker.Migrations.dll"]
```

### Build + push (AKS requires linux/amd64)

Use a unique tag (avoid caching pain):

```bash
TAG=dev-$(date +%Y%m%d-%H%M%S)

docker buildx build \
  --platform linux/amd64 \
  -t acrexptrackerhqdev01.azurecr.io/expense-migrator:$TAG \
  -f apps/migrations/ExpenseTracker.Migrations/Dockerfile \
  --push \
  .
```

### Why I stopped using “same tag”

I hit a classic issue:
- same tag reused
- node cached image
- `imagePullPolicy: IfNotPresent`

So AKS didn’t pull our new digest.

✅ Fix options:
- best: use unique tags
- alternative: set pull policy Always

---

## Helm Hook Job (runs migrations before API)

### Values: `infra/helm/expense-api/values-expense-dev.yaml`

Add:

```yaml
migrations:
  enabled: true
  image:
    repository: acrexptrackerhqdev01.azurecr.io/expense-migrator
    tag: "<TAG_FROM_PUSH>"

workloadIdentity:
  clientId: "<CLIENT_ID_GUID>"

keyVault:
  uri: "https://expensetracker-dev-kv.vault.azure.net/"

podLabels:
  azure.workload.identity/use: "true"

serviceAccount:
  create: true
  name: expense-api
  annotations:
    azure.workload.identity/client-id: "<CLIENT_ID_GUID>"
```

### Template: `infra/helm/expense-api/templates/migrations-job.yaml`

```yaml
{{- if .Values.migrations.enabled }}
apiVersion: batch/v1
kind: Job
metadata:
  name: expense-api-migrations
  labels:
    {{- include "expense-api.labels" . | nindent 4 }}
  annotations:
    "helm.sh/hook": pre-install,pre-upgrade
    "helm.sh/hook-delete-policy": before-hook-creation,hook-succeeded
spec:
  backoffLimit: 1
  template:
    metadata:
      labels:
        app: expense-api-migrations
        {{- with .Values.podLabels }}
        {{- toYaml . | nindent 8 }}
        {{- end }}
      {{- with .Values.podAnnotations }}
      annotations:
        {{- toYaml . | nindent 8 }}
      {{- end }}
    spec:
      restartPolicy: Never
      serviceAccountName: {{ .Values.serviceAccount.name | default (include "expense-api.serviceAccountName" .) }}
      containers:
        - name: migrator
          image: "{{ .Values.migrations.image.repository }}:{{ .Values.migrations.image.tag }}"
          imagePullPolicy: IfNotPresent
          env:
            - name: KeyVault__Uri
              value: "{{ .Values.keyVault.uri }}"
            - name: AZURE_CLIENT_ID
              value: "{{ .Values.workloadIdentity.clientId }}"
{{- end }}
```

### Deploy

```bash
helm upgrade --install expense-api infra/helm/expense-api -n expense-dev -f infra/helm/expense-api/values-expense-dev.yaml
```

### If hook job fails (BackoffLimitExceeded)

Get logs from the *pod* created by the job:

```bash
kubectl get pods -n expense-dev --sort-by=.metadata.creationTimestamp | tail -n 20
kubectl logs -n expense-dev pod/<expense-api-migrations-pod> -c migrator
kubectl describe pod -n expense-dev <expense-api-migrations-pod>
```

> Note: `kubectl logs --previous` often won’t work for Jobs because containers usually don’t restart.

### Why logs sometimes say “job not found” after success

Because hook delete policy includes `hook-succeeded`. Helm deletes the job after success.

---

## Final Verification Checklist

### 1) API can connect and tables exist

Swagger call should no longer return:
- `Invalid object name 'Expenses'`

Instead, requests should succeed (200/201).

### 2) No DB secrets in pod env

```bash
kubectl exec -n expense-dev deploy/expense-api -c expense-api -- env | grep -E "ConnectionStrings|Password|User ID" || echo "✅ no DB secrets in env"
```

### 3) DNS resolves privately from inside cluster

```bash
kubectl run -n expense-dev dns-check --rm -it --image=busybox:1.36 --restart=Never -- sh
nslookup <sql-server>.database.windows.net
```

Expect private IP.

---

## Summary (What I achieved)

By the end of Sprint 4.2:

- ✅ SQL is reachable privately via Private Endpoint + Private DNS
- ✅ DB credentials exist only in Azure Key Vault
- ✅ AKS pods authenticate to Key Vault via Workload Identity (OIDC)
- ✅ No secrets in Git/Helm/manifests/env
- ✅ EF migrations are applied automatically via Helm-hook Kubernetes Job
- ✅ API can read/write data to Azure SQL

This is a production-grade pattern.

```md
"No secrets. Identity-driven access. Private networking. Repeatable migrations." ✅
```
