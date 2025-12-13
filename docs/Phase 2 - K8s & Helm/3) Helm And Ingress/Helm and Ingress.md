# Sprint 2.3 — Helm + Ingress + Local Routing (Deep-Dive Documentation)
Expense Tracker — Phase 2 (Containerization & Local Kubernetes)

---

## 📌 Sprint Goal

Convert raw Kubernetes manifests into a production-ready Helm chart, deploy the Expense Tracker API using Helm, and expose it through an NGINX Ingress Controller using a friendly local hostname (`expense.local`).

This sprint introduced several advanced topics:

- Helm templating  
- Chart structure and values injection  
- Ingress and host-based routing  
- NodePort vs LoadBalancer behavior on local clusters  
- Port-forwarding the ingress controller (why it is required on macOS)

This document explains each step in depth so that it is fully understood and repeatable.

---

# 🧠 Part 1 — Understanding Helm

## ❓ What Is Helm?

Helm is the **package manager for Kubernetes**, similar to:
- npm for JavaScript  
- NuGet for C#  
- apt for Debian/Ubuntu  

Instead of installing libraries, Helm installs **Kubernetes applications**.

A Helm chart bundles:
- Deployments  
- Services  
- ConfigMaps  
- Secrets  
- Ingress  
- Values  
- All templates required for installation  

Into **one reusable, versioned unit**.

### Why Use Helm?

Without Helm:
- You manually manage many YAML files  
- Upgrades require deleting/reapplying manifests  
- No history tracking  
- No rollback  
- No environment-level parameters  

With Helm:
- `helm upgrade` applies changes to the running release  
- `helm history` shows past versions  
- `helm rollback` safely reverts to previous versions  
- `values.yaml` controls environment-specific configuration  
- Charts are portable and usable across dev, test, and production  

In any professional cloud-native project, Helm is essential.

---

# 🧠 Part 2 — Creating the Helm Chart

Command:

```
helm create expense-api
```

This generated:

```
charts/expense-api/
  Chart.yaml
  values.yaml
  templates/
    deployment.yaml
    service.yaml
    configmap.yaml
    secret.yaml
    ingress.yaml
    serviceaccount.yaml
```

### Purpose of Each File

| File | Description |
|------|-------------|
| Chart.yaml | Metadata about the chart |
| values.yaml | Default values injected into templates |
| templates/ | Kubernetes YAML templates |
| templates/_helpers.tpl | Shared functions and named templates |

Everything inside `templates/` becomes **real YAML** when rendered.

---

# 🧠 Part 3 — Moving Templates Into Helm

We replaced Helm’s default templates with our real Kubernetes resources.

Hard-coded values such as:

```
image: marcobiundo/expense-api:v2
```

became templated values:

```
image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
```

### Why?

Different environments (dev, test, prod) may use different:
- image repositories
- tags
- replica counts
- resource limits
- hostnames

Instead of modifying YAML files directly, we override values in:
- `values.yaml`
- or via `--set` flags

This makes deployments predictable and standardized.

---

# 🧠 Part 4 — Running Helm Lint

Validation:

```
helm lint ./charts/expense-api
```

Output:

```
0 chart(s) failed
```

Meaning the chart:
- contained no syntax issues  
- was ready for deployment  

---

# 🧠 Part 5 — Deploying With Helm

```
helm upgrade --install expense-api ./charts/expense-api -n expense-dev
```

Helm actions performed:
1. Render templates → real YAML  
2. Compare with last release  
3. Apply changes  
4. Record release history  

Verification:

```
kubectl get pods -n expense-dev
kubectl get svc -n expense-dev
```

Deployment successful.

---

# 🧠 Part 6 — Understanding Ingress

## ❓ What is an Ingress?

Ingress is the **HTTP/HTTPS entry point** into your Kubernetes cluster.

Instead of exposing each service manually, you expose **one ingress controller**, and configure routing rules such as:

```
expense.local → expense-api service → pods
```

### NGINX Ingress Is a Reverse Proxy

The ingress controller:
- listens on ports 80/443  
- receives external HTTP requests  
- forwards them based on Ingress rules  
- provides routing, TLS, and URL path handling  

Ingress is essential for real microservice deployments.

---

# 🧠 Part 7 — Why the Ingress Service Shows `<pending>`

Your ingress service is:

```
ingress-nginx-controller   LoadBalancer   <pending>
```

This is because:

- Kubernetes thinks it is in a cloud environment  
- It requests a **cloud load balancer**  
- Docker Desktop is **not a cloud provider**  
- So no external IP can be allocated  

In AKS:
- this will automatically become a public IP
- and no special steps are required

Locally:
- You do not get a real external IP
- The only option is **NodePort** or **port-forward**

---

# 🧠 Part 8 — Why NodePort Didn’t Work on macOS

Your NodePort mapping:

```
80:31208/TCP
```

means:

- Inside the Kubernetes VM node: port 31208 routes to port 80
- On macOS: Docker Desktop does NOT expose that port on your host network

So:

```
curl localhost:31208 → connection refused
```

This is normal.  
The NodePort exists **inside the Linux VM**, not on your Mac.

---

# 🧠 Part 9 — Port-Forwarding the Ingress Controller

To actually reach ingress, we create a manual tunnel:

```
kubectl port-forward svc/ingress-nginx-controller 8080:80 -n ingress-nginx
```

This exposes:
- `localhost:8080` on your Mac  
- to port **80** on the ingress controller service inside the cluster  

Now the routing chain becomes:

```
http://expense.local:8080
 → /etc/hosts resolves to 127.0.0.1
 → port-forward connects to ingress controller
 → ingress routing sends request to expense-api service
 → which load-balances to pods
```

This is how local ingress works without a real cloud load balancer.

When deployed to AKS:
- No port-forwarding  
- No NodePort  
- A public external IP solves everything  

---

# 🧠 Part 10 — Validating Ingress Functionality

Once port-forwarding was active, these URLs succeeded:

```
http://expense.local:8080/health
http://expense.local:8080/swagger
```

This confirms:
- DNS → correct  
- Ingress → routing correctly  
- Service → reachable  
- Pods → healthy  
- Helm deployment → working  

This completes Sprint 2.3.

---

# ✅ Sprint 2.3 Completion Checklist

| Task | Status |
|------|--------|
| Create Helm chart | ✔ |
| Move YAML templates into chart | ✔ |
| Configure values.yaml | ✔ |
| Run helm lint | ✔ |
| helm upgrade --install | ✔ |
| Verify pods and services | ✔ |
| Install NGINX ingress controller | ✔ |
| Create Ingress resource | ✔ |
| Update /etc/hosts | ✔ |
| Validate ingress (via port-forward) | ✔ |

---

# 🏁 Final Summary — What Was Learned

Sprint 2.3 introduced critical cloud-native skills:

### ✔ Helm Chart Creation  
You now understand how to template Kubernetes objects for reuse.

### ✔ Environment-Aware Deployments  
values.yaml enables dev/test/prod pipelines.

### ✔ Ingress + Host-Based Routing  
Core skill for microservices and AKS traffic management.

### ✔ Local Kubernetes Networking Constraints  
LoadBalancer cannot allocate external IP on macOS → NodePort unusable → port-forward required.

### ✔ Port-Forwarding Ingress  
The final bridge allowing local testing of ingress-based routing.

This sprint transforms your project from “running containers” into a **cloud-ready Kubernetes workload**.

You are fully prepared to move into **Phase 3 — Azure Foundations + Terraform**.
