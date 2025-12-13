# Kubernetes Secret – Expense API (Line‑by‑Line Explanation)

This document explains exactly how the `expense-api-secret.yaml` file works.  
By the end, I should fully understand what a Secret is, why it exists, and how .NET reads values from it.

---

## Full YAML for Reference (Do NOT edit here)

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

---

# 🔐 What a Kubernetes Secret Actually Is

A **Secret** is a Kubernetes object that stores **sensitive key/value data**, such as:

- passwords  
- tokens  
- database connection strings  
- certificates  

It works similarly to a ConfigMap, but is intended for **private** values.  
Kubernetes automatically hides their contents and restricts access to them.

Secrets become **environment variables inside the container**, and .NET reads them just like any other configuration.

---

# Line‑by‑Line Breakdown

## `apiVersion: v1`

All Secrets live in the **core** Kubernetes API group.  
The stable version for these objects is `v1`.

This tells Kubernetes how to parse and validate the object.

---

## `kind: Secret`

This declares the Kubernetes resource type.  
The cluster will treat this YAML as a **Secret object** rather than a Deployment, Service, etc.

---

## `metadata`

```yaml
metadata:
  name: expense-api-secrets              
  labels:
    app: expense-api 
```

### `name: expense-api-secrets`

This is the **name** of the Secret.

Your Deployment references this name when injecting secrets:

```yaml
envFrom:
  - secretRef:
      name: expense-api-secrets
```

If the names don't match exactly, Kubernetes cannot find the Secret and your pod will fail.

### `labels`

Labels are metadata used for:
- filtering resources  
- grouping objects  
- associating related items during debugging or tooling  

Labeling Secrets with `app: expense-api` keeps your objects consistent and easy to track.

---

## `type: Opaque`

This tells Kubernetes the Secret holds **arbitrary** key/value pairs.

Types of Secrets include:

- `Opaque` → generic secrets (most common)  
- `kubernetes.io/dockerconfigjson` → registry credentials  
- `kubernetes.io/tls` → SSL certificates  

For connection strings and passwords, `Opaque` is correct.

---

## `stringData:` (VERY important)

```yaml
stringData:
  ConnectionStrings__ExpenseTrackerDb: "Server=placeholder;Database=ExpenseTrackerDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"
```

### Why `stringData`?

Because it allows **plain text values** and Kubernetes automatically handles base64 encoding under the hood.

If you used `data:` instead:
- you would need to manually base64‑encode values
- YAML becomes unreadable

`stringData` = easier authoring, same security in the cluster.

### The Key Name (And Why It Looks Weird)

`ConnectionStrings__ExpenseTrackerDb`

Double-underscore (`__`) is how **.NET maps environment variables to nested config sections**.

It becomes:

```json
"ConnectionStrings": {
  "ExpenseTrackerDb": "Server=...;"
}
```

Which means your .NET code can call:

```csharp
builder.Configuration.GetConnectionString("ExpenseTrackerDb")
```

And it will automatically retrieve the value from the Secret.

### The Value

The placeholder string is a full connection string — exactly like what EF Core or SQLClient expects.

In production, this would be replaced with:

- a secure DB hostname  
- real user credentials  
- SSL options  

But for Kubernetes learning, a placeholder is fine.

---

# How Secrets Become Environment Variables in the Pod

Your Deployment uses:

```yaml
envFrom:
  - secretRef:
      name: expense-api-secrets
```

This means **all keys in the Secret become env vars** inside the container.

Inside your running pod, if you run:

```bash
env | grep Connection
```

You would see:

```
ConnectionStrings__ExpenseTrackerDb=Server=...
```

Then .NET merges that into its configuration system seamlessly.

---

# How .NET Uses This Secret

In Program.cs, when your application starts:

- `.NET automatically loads environment variables into IConfiguration`
- Double underscores (`__`) become nested sections
- ConnectionStrings get special mapping

So this works:

```csharp
var connection = builder.Configuration.GetConnectionString("ExpenseTrackerDb");
```

Your app never knows the value came from Kubernetes — it just sees configuration.

---

# Why Secrets Matter Before AKS

Even though you don’t have SQL running in Kubernetes yet:

✔ this teaches you the wiring  
✔ your Deployment is now production‑grade  
✔ you’re preparing for AKS + Key Vault  
✔ this follows 12‑Factor App principles  
✔ your config is now externalized, not hardcoded  

This is required before:
- Helm charts  
- Ingress controllers  
- Terraform + AKS deployments  
- Azure Key Vault integration  

---

# Summary (Key Things to Remember)

- Secrets store **sensitive** values (ConfigMaps store safe values)
- `stringData:` lets you write values normally — K8s handles encoding
- Keys become **environment variables** in your container
- `.NET` uses `__` to map env vars → nested config
- Deployment references Secrets using `envFrom.secretRef`
- This is a foundational DevOps skill before moving to Helm and AKS

You should now be able to write a Kubernetes Secret **from scratch** and wire it to an ASP.NET Core container confidently.
