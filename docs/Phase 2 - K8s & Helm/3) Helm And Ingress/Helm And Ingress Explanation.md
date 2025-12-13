# Helm & Ingress Deep-Dive  
_A complete conceptual explanation for Phase 2_

This document explains **Helm**, **Ingress**, and **NGINX Ingress Controller** in a way that prepares you for real-world Kubernetes and AKS (Azure Kubernetes Service).  
It is meant for *study*, not just reference — so it contains detailed explanations, diagrams (described), and practical examples.

---

# 🧠 PART 1 — What is HELM?

## ❓ Why does Helm exist?

Kubernetes YAML gets messy fast.

A simple microservice typically requires:
- Deployment  
- Service  
- ConfigMap  
- Secret  
- Ingress  
- HorizontalPodAutoscaler  
- ServiceAccount  
- RBAC permissions  
- Environment-specific values (dev/test/prod)  

If you manage these manually:
- You repeat yourself  
- You copy/paste YAML  
- Small changes require editing many files  
- No versioning per deployment  
- No rollback  
- No history  

Helm was created to solve those exact problems.

---

# 🎁 Helm = Kubernetes Package Manager

Just like:
- **npm** installs JavaScript packages  
- **NuGet** installs .NET libraries  
- **pip** installs Python packages  

Helm *installs Kubernetes applications*.

A Helm “chart” is a packaged application:
```
chart/
  Chart.yaml
  values.yaml
  templates/*.yaml
```

When you run:

```
helm upgrade --install myapp ./chart
```

Helm:

1. Reads the templates  
2. Applies your values  
3. Renders real Kubernetes YAML  
4. Applies it to the cluster  
5. Tracks it as a versioned release  

You can think of Helm as:

> **Terraform for Kubernetes manifests.**

---

# 🏗️ Helm Chart Structure

A chart created by `helm create mychart` looks like:

```
mychart/
  Chart.yaml
  values.yaml
  templates/
    deployment.yaml
    service.yaml
    ingress.yaml
    configmap.yaml
    secret.yaml
    _helpers.tpl
```

### **Chart.yaml**
Metadata:
- chart name  
- version  
- description  

### **values.yaml**
Default configuration for:
- image repository  
- image tag  
- replicas  
- resources  
- ingress hostnames  

This is the heart of environment customization.

### **templates/**
Contains *templated* Kubernetes manifests.

Example:

```yaml
image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
```

This becomes:

```yaml
image: "marcobiundo/expense-api:v2"
```

based on `values.yaml`.

---

# 🔥 Why Helm Is the Standard in Kubernetes

## ✔ Environment Separation
Dev can use:
```
image.tag = v3-dev
replicas = 1
```

Prod can use:
```
image.tag = v3
replicas = 4
```

No YAML duplication — just override values.

---

## ✔ Versioned Releases

Every install = a new revision:

```
helm history expense-api
```

Rollback instantly:

```
helm rollback expense-api 1
```

This is **critical for production**.

---

## ✔ Repeatable Deployments

Your chart is reusable across:
- Minikube  
- Docker Desktop cluster  
- AKS (Azure)  
- EKS (AWS)  
- GKE (Google)  

No changes needed — Kubernetes is Kubernetes.

---

## ✔ Templates Reduce Errors

Instead of editing 5 YAML files to bump image versions:

```
helm upgrade expense-api --set image.tag=v5
```

One command → entire deployment updated.

---

# 🧠 PART 2 — What Is an INGRESS?

Services in Kubernetes expose your app in three ways:

| Type | Purpose |
|------|---------|
| **ClusterIP** | internal-only traffic inside cluster |
| **NodePort** | opens a high port (30000–32767) on each node |
| **LoadBalancer** | gets a public IP (cloud provider only) |

But microservices need more:
- Host-based routing  
- Path routing  
- TLS termination  
- Pretty URLs  
- A single public IP  

This is where **Ingress** comes in.

---

# 🌐 Ingress = Kubernetes HTTP Router

You define rules like:

```yaml
host: expense.local
path: /
service: expense-api
port: 80
```

Meaning:

> “When someone visits `expense.local`, forward that HTTP request to the `expense-api` Service.”

Ingress itself is **just the rules**.  
Ingress does NOT process traffic on its own.

You need an **Ingress Controller** to actually do the routing.

---

# 🧠 PART 3 — What Is the NGINX INGRESS CONTROLLER?

Ingress Controller = the *actual software* behind Ingress.

Kubernetes doesn’t ship with an ingress controller by default.

NGINX is the most popular implementation.

### What NGINX Ingress Controller does:

- Listens on port 80 and 443  
- Reads all Ingress objects  
- Routes traffic based on Host/Path rules  
- Handles TLS termination  
- Performs load balancing to backend pods  
- Supports annotations for advanced config  

When you install it:

```
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx
```

You are deploying:
- A Deployment (the controller pod)
- A LoadBalancer/NodePort service
- Configuration needed to watch Ingress objects

---

# 🔁 How NGINX Ingress Processes a Request

Imagine you browse:

```
http://expense.local/swagger
```

Flow:

```
Browser
  ↓
Local DNS (/etc/hosts → 127.0.0.1)
  ↓
Ingress Controller (port-forward or LoadBalancer)
  ↓
Ingress rule checks:
       host == expense.local ?
       path == / ?
  ↓
expense-api Service (ClusterIP)
  ↓
Pod (your API container)
```

The controller is essentially a powerful reverse proxy inside the cluster.

---

# 🧠 PART 4 — Why LoadBalancer Shows `<pending>` on macOS

The service created by ingress-nginx is:

```
Type: LoadBalancer
EXTERNAL-IP: <pending>
```

Because Docker Desktop is NOT a cloud provider:
- No Azure Load Balancer  
- No AWS ELB  
- No Google GLB  

So Kubernetes can’t provision an external IP.

This is normal and expected.

---

# 🔧 PART 5 — Why NodePort Didn’t Work on macOS

The ingress-nginx service exposes:

```
80:31208/TCP
```

Meaning:

- Inside the Kubernetes VM node → port 31208 works  
- On macOS → NOT directly accessible  

Your Mac is NOT the Kubernetes node.  
The node is a Linux VM inside Docker Desktop.

Thus:

```
curl localhost:31208
→ connection refused
```

---

# 🔌 PART 6 — Why Port-Forwarding the Ingress Service Works

Command:

```
kubectl port-forward svc/ingress-nginx-controller 8080:80 -n ingress-nginx
```

This creates a tunnel:

| Local Mac | Kubernetes |
|----------|------------|
| localhost:8080 | ingress controller port 80 |

Now the entire routing chain can work locally.

---

# 🧪 Example Request Flow With Port-Forwarding

Browser → `http://expense.local:8080/swagger`

1. `/etc/hosts` maps `expense.local` → 127.0.0.1  
2. Port-forward intercepts traffic on port 8080  
3. Traffic enters ingress controller  
4. Ingress object routes request to expense-api service  
5. Service load-balances to one of your pods  
6. Pod returns response → back through ingress → to browser  

This fully mimics cloud routing.

---

# 🔥 PART 7 — Why This Matters for Phase 3 (AKS)

When you deploy to Azure Kubernetes Service:

- The same Helm chart will work  
- The Ingress Controller will receive a real public IP  
- No port-forwarding  
- No NodePort limitations  
- DNS can map `api.yourdomain.com` → external IP  

Everything learned here **directly applies** to real production workloads.

---

# 🧠 FINAL SUMMARY

### ✔ Helm
- Package manager for Kubernetes apps  
- Templating + values = reusable deployments  
- Versioned releases & rollback  
- Industry standard for app deployment  

### ✔ Ingress
- Defines HTTP routing rules  
- Handles host/path traffic mapping  
- Requires a controller to function  

### ✔ NGINX Ingress Controller
- Listens on 80/443  
- Reads Ingress resources  
- Routes traffic to services  
- Most widely used controller in the Kubernetes ecosystem  

### ✔ Local Networking (macOS)
- LoadBalancer cannot be provisioned  
- NodePort isn’t accessible directly  
- Port-forwarding simulates real ingress entrypoint  

---

This file gives you the conceptual grounding needed to move confidently into **Phase 3 — Azure Foundations + Terraform + AKS**.
