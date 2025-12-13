# Phase 2.2 — Kubernetes Config & Secrets  
A step-by-step record of WHAT I did, HOW I did it, and WHY it matters.

This file documents every action taken in Sprint 2.2 as I added configuration management to my Kubernetes deployment using **ConfigMaps**, **Secrets**, and **envFrom** in the Deployment.

It is written to help future‑me understand not only *the commands* but *the reasoning behind each step*.

---

# ⭐ Overview of Sprint 2.2

The goal of Sprint 2.2 was to:

- Externalize all application settings (stop hardcoding values in Deployment)
- Move non-secret configuration into a **ConfigMap**
- Move sensitive values (like connection strings) into a **Secret**
- Update the Deployment to load values from Kubernetes instead of inline environment variables
- Validate the API still runs with `/health` and `/swagger`

This prepares the app for:
- Helm charts  
- multi‑environment deployments  
- AKS  
- Azure Key Vault  
- 12‑Factor App compliance  

---

# 🧩 Step 1 — Create the ConfigMap

## Why I needed this
A Deployment should **not** contain environment-specific values.  
Instead, Kubernetes best practice is:

- Use a **ConfigMap** for non-secret settings  
- Use a **Secret** for sensitive settings  

This allows per-environment configuration without rebuilding containers or redeploying YAML.

ASP.NET Core reads environment variables automatically, so moving them into a ConfigMap fits perfectly.

## What I did
Created a file:

```
k8s/expense-api-configmap.yaml
```

Contents:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: expense-api-config
  labels:
    app: expense-api
data:
  ASPNETCORE_ENVIRONMENT: "Development"
  RunMigrations: "false"
```

## Command I ran

```bash
kubectl apply -f k8s/expense-api-configmap.yaml
```

## Result
The ConfigMap was created and verified with:

```bash
kubectl get configmap -n expense-dev
kubectl describe configmap expense-api-config -n expense-dev
```

I now have environment settings controlled by Kubernetes, not my Deployment.

---

# 🧩 Step 2 — Create the Secret

## Why I needed this
Connection strings **should never** go into:

- Deployment YAML  
- Git repo  
- Dockerfile  
- ConfigMap  

So Kubernetes Secrets exist to provide secure storage for sensitive configuration.

.NET maps environment variables containing `__` (double-underscore) into nested config keys.

That means:

```
ConnectionStrings__ExpenseTrackerDb
```

becomes:

```json
"ConnectionStrings": {
  "ExpenseTrackerDb": "value"
}
```

## What I did

Created this file:

```
k8s/expense-api-secret.yaml
```

Contents:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: expense-api-secrets
  labels:
    app: expense-api
type: Opaque
stringData:
  ConnectionStrings__ExpenseTrackerDb: "Server=placeholder;Database=ExpenseTrackerDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"
```

## Command I ran

```bash
kubectl apply -f k8s/expense-api-secret.yaml
```

## Result

Verified with:

```bash
kubectl get secrets -n expense-dev
kubectl describe secret expense-api-secrets -n expense-dev
```

Kubernetes stored the connection string securely and hid the actual value.

---

# 🧩 Step 3 — Update Deployment to Use ConfigMap + Secret

## Why this was required
Originally, the Deployment contained inline:

```yaml
env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Development"
  - name: RunMigrations
    value: "false"
```

This violates:
- Mutability (changing config requires editing Deployment)
- 12‑Factor App principles
- Separation of configuration from code
- Kubernetes best practices

The correct approach is:

```yaml
envFrom:
  - configMapRef:
      name: expense-api-config
  - secretRef:
      name: expense-api-secrets
```

This loads **all** ConfigMap and Secret keys into environment variables automatically.

## What I changed

In:

```
k8s/expense-api-deployment.yaml
```

I removed the entire `env:` section and replaced it with:

```yaml
envFrom:
  - configMapRef:
      name: expense-api-config
  - secretRef:
      name: expense-api-secrets
```

## Command I ran

```bash
kubectl apply -f k8s/expense-api-deployment.yaml
kubectl rollout status deployment/expense-api -n expense-dev
kubectl get pods -n expense-dev
```

## Result
The Deployment rolled out successfully.  
New pods launched with:

- ConfigMap values as env vars  
- Secret values as env vars  

The app started without errors.

---

# 🧩 Step 4 — Validate Setup (Port Forward + Health)

## Why
To ensure:
- Probes are still working  
- ASP.NET Core successfully loads environment values  
- Container still boots with no inline config  

## Command I ran

```bash
kubectl port-forward svc/expense-api 5000:80 -n expense-dev
```

Verified:

- `http://localhost:5000/health` → 200 OK  
- `http://localhost:5000/swagger` → Loads normally  

## Result
The app runs exactly as before, but now reads all configuration from Kubernetes instead of Deployment YAML.

This is production‑grade behavior.

---

# 🎉 Sprint 2.2 Completed

✔ ConfigMap created  
✔ Secret created  
✔ Deployment updated  
✔ Pods rolled out successfully  
✔ API works via port forward  
✔ All configuration externalized  
✔ App is now fully Kubernetes‑compliant  

This puts me in perfect position for:

- Ingress + DNS  
- Helm chart generation (Phase 2 final stage)  
- Multi‑environment deployments  
- AKS + Terraform later in Phase 3  

---

# 🧠 What I Learned

- ConfigMaps store non-secret config  
- Secrets store sensitive values  
- `envFrom` injects an entire ConfigMap/Secret into the container  
- ASP.NET Core maps `__` to nested keys  
- Configuration should always live *outside* the Deployment  
- Kubernetes becomes the source of truth for runtime behavior  
- This is exactly how real enterprise apps are configured  

---

This document is now your single source of truth for all decisions, commands, and reasoning behind Sprint 2.2.
