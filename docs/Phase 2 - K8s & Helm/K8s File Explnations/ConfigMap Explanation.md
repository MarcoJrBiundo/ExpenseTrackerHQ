# Kubernetes ConfigMap – Expense API (Line-by-Line Explanation)

This document explains the `expense-api-configmap.yaml` file in clear, practical terms so future-me can recreate it from scratch and understand exactly how ConfigMaps work inside Kubernetes.

---

## Full YAML for Reference (Do NOT edit here)

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

---

# 🧠 What a ConfigMap Actually Is

A **ConfigMap** stores **non-sensitive configuration values** for an application.  
Think of it like `appsettings.json`, but stored *outside* your container so you don’t have to rebuild the image every time config changes.

Examples of good ConfigMap values:
- ASPNETCORE_ENVIRONMENT
- feature flags
- logging levels
- app settings that aren’t secrets

A ConfigMap becomes **environment variables** inside your container and .NET treats them the same as any other configuration source.

---

# Line-by-Line Explanation

## `apiVersion: v1`
ConfigMaps belong to Kubernetes’ core API group.  
The stable version for this group is `v1`.

---

## `kind: ConfigMap`
Declares the resource **type**.  
Kubernetes will treat this YAML as a ConfigMap object.

---

## `metadata`

```yaml
metadata:
  name: expense-api-config
  labels:
    app: expense-api
```

### `name: expense-api-config`
The name of this ConfigMap.

Your Deployment references it using:

```yaml
envFrom:
  - configMapRef:
      name: expense-api-config
```

If this name doesn't match exactly, Kubernetes cannot load the config.

### `labels`
Labels help identify related objects.  
Here, `app: expense-api` lets you group and filter your resources easily.

---

## `data:` – Key/Value Configuration

This is where all non-secret configuration lives.

```yaml
data:
  ASPNETCORE_ENVIRONMENT: "Development"
  RunMigrations: "false"
```

Each entry becomes an **environment variable** inside your running container.

### `ASPNETCORE_ENVIRONMENT: "Development"`
Controls how ASP.NET Core behaves:

- which `appsettings.*.json` file is used  
- whether developer exception pages appear  
- whether Swagger auto-loads  

When Kubernetes injects this value, your container behaves exactly like running locally with:

```bash
export ASPNETCORE_ENVIRONMENT=Development
```

### `RunMigrations: "false"`
Your API checks this value on startup:

```csharp
var runMigrations = configuration.GetValue<bool>("RunMigrations");
```

By storing this in a ConfigMap, Kubernetes now decides whether your container should run migrations or not.

In Phase 2:
- We **disable** migrations because SQL isn’t running inside the cluster yet.

Later in AKS:
- We may enable migrations in CI/CD jobs instead of inside the pod.

---

# How ConfigMaps Become Environment Variables

Your Deployment uses:

```yaml
envFrom:
  - configMapRef:
      name: expense-api-config
```

This means:

➡️ **All keys in the ConfigMap become environment variables** inside the pod.

Putting it together:

| ConfigMap Key | Environment Variable Inside Pod |
|---------------|---------------------------------|
| ASPNETCORE_ENVIRONMENT | ASPNETCORE_ENVIRONMENT=Development |
| RunMigrations | RunMigrations=false |

.NET automatically reads these at startup.

---

# How .NET Uses ConfigMap Values

ASP.NET Core merges environment variables into the `IConfiguration` system.

That means this works:

```csharp
builder.Configuration["RunMigrations"];
builder.Configuration["ASPNETCORE_ENVIRONMENT"];
```

And when double-underscore (`__`) is used (for Secrets):

```csharp
builder.Configuration.GetConnectionString("ExpenseTrackerDb");
```

ASP.NET Core doesn't care whether values came from:
- appsettings.json  
- environment variables  
- ConfigMaps  
- Secrets  
- Azure Key Vault  

Everything flows into the same configuration tree.

---

# Why You Externalize Config (12-Factor Principle)

This move aligns your app with modern cloud practices:

### ✔ No rebuilding container images to change config  
### ✔ No environment-specific code  
### ✔ No hardcoded values inside Deployment YAML  
### ✔ Easily swapped per environment (dev → qa → prod)  
### ✔ Fully compatible with Helm + AKS later  

Kubernetes now owns configuration, not your code.

---

# Summary (Key Points to Remember)

- ConfigMaps hold **non-secret** configuration.
- All values under `data:` become environment variables inside the container.
- Deployment references it using `envFrom.configMapRef`.
- ASP.NET Core automatically reads these env vars into its configuration system.
- This is a required foundation before:
  - Helm templates  
  - multi-environment deployments  
  - AKS + Terraform  

With this, you should be able to explain ConfigMaps to anyone and recreate them without referencing prior code.
