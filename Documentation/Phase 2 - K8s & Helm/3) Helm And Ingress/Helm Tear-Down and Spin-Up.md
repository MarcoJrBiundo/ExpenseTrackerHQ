# Helm Tear Down and Start Up
_Expense Tracker — Phase 2 (K8s & Helm)_

This guide explains **how to tear down** everything related to the Expense API in Kubernetes and **how to spin it back up from scratch** using Helm and Ingress.

Use this when:
- You want a clean slate.
- You haven’t touched the project in a while and want a step-by-step startup.
- You’re debugging cluster issues and want to be sure everything is recreated correctly.

---

## 🧱 Prerequisites

Before running anything here, make sure:

- Docker Desktop is running.
- Kubernetes is enabled in Docker Desktop.
- You are in the **ExpenseTracker.Api** directory when running Helm commands:

```bash
cd /Users/marcobiundo/Documents/ExpenseTracker.Api
```

Namespaces used:
- **expense-dev** → app namespace (API, Service, ConfigMap, Secret, Ingress)
- **ingress-nginx** → NGINX Ingress Controller namespace


---

## 🧨 Part 1 — Tear Down

### Option A — Tear Down Only the Expense API (Keep Ingress Controller)

Use this when you want to:
- Remove the app
- But keep the ingress-nginx controller installed for other work

**Commands:**

1. Uninstall the Helm release for the Expense API:

```bash
helm uninstall expense-api -n expense-dev
```

2. Verify everything is gone from the `expense-dev` namespace:

```bash
kubectl get all -n expense-dev
kubectl get configmap -n expense-dev
kubectl get secret -n expense-dev
kubectl get ingress -n expense-dev
```

You should see either **no resources** or only system defaults like `kube-root-ca.crt`.


### Option B — Full Tear Down (App + Ingress Controller)

Use this when you want to:
- Reset everything Phase 2 created
- Simulate a totally fresh cluster state

**Commands:**

1. Uninstall the Expense API release:

```bash
helm uninstall expense-api -n expense-dev
```

2. Uninstall the NGINX Ingress Controller release:

```bash
helm uninstall ingress-nginx -n ingress-nginx
```

3. (Optional) Delete namespaces if you want them fully removed:

```bash
kubectl delete namespace expense-dev
kubectl delete namespace ingress-nginx
```

4. Verify namespaces and resources:

```bash
kubectl get namespaces
kubectl get all -A
```

At this point there should be **no app pods, services, ingress, or ingress-nginx controller**.

---

## 🚀 Part 2 — Start Up From Scratch

Use this when you want to:
- Bring the system back online
- After a tear down
- Or when starting a new dev session

We assume you are in:

```bash
cd /Users/marcobiundo/Documents/ExpenseTracker.Api
```

And that namespaces either exist or will be created as needed.


### Step 1 — Ensure/Prepare Namespaces

If `expense-dev` does not exist yet:

```bash
kubectl create namespace expense-dev
```

You do **not** need to manually create `ingress-nginx` — Helm will handle it with `--create-namespace`.


### Step 2 — Install NGINX Ingress Controller (If Needed)

If you tore it down or are on a fresh cluster:

1. Add the ingress-nginx Helm repo (only needed once per machine):

```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
```

2. Install (or upgrade) the ingress-nginx controller:

```bash
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
  -n ingress-nginx --create-namespace
```

3. Verify ingress controller pods and services:

```bash
kubectl get pods -n ingress-nginx
kubectl get svc -n ingress-nginx
```

You should see a controller pod similar to:

```text
ingress-nginx-controller   1/1   Running
```


### Step 3 — Deploy the Expense API via Helm

From `ExpenseTracker.Api` root:

```bash
helm upgrade --install expense-api ./charts/expense-api -n expense-dev
```

This will:
- Render the chart templates
- Create/Update:
  - Deployment (expense-api)
  - Service (expense-api)
  - ConfigMap (expense-api-config)
  - Secret (expense-api-secrets)
  - Ingress (expense-api)

Verify resources:

```bash
kubectl get pods -n expense-dev
kubectl get svc -n expense-dev
kubectl get ingress -n expense-dev
```

Expected:
- 2 pods for `expense-api` in `Running` state
- A ClusterIP service `expense-api` on port 80
- An Ingress named `expense-api` with host `expense.local`


### Step 4 — Ensure /etc/hosts Has `expense.local`

On macOS, the local hostname mapping is in `/etc/hosts`.

Entry should include:

```text
127.0.0.1   expense.local
```

To edit:

```bash
sudo nano /etc/hosts
```

Add the line if it is missing, then save and exit.


### Step 5 — Port-Forward the Ingress Controller (Local Dev Only)

Because Docker Desktop cannot provision a real cloud LoadBalancer, the ingress service external IP stays `<pending>`. NodePort is not directly reachable from macOS, so we **port-forward the ingress controller** to make local access work.

Run:

```bash
kubectl port-forward svc/ingress-nginx-controller 8080:80 -n ingress-nginx
```

Keep this terminal **open** while you are working. This binds:

- `localhost:8080` on your Mac
- to port **80** on the ingress controller service inside the cluster

With `/etc/hosts` set up, these URLs should now work:

```text
http://expense.local:8080/health
http://expense.local:8080/swagger
```

This confirms:
- DNS mapping from `expense.local` → 127.0.0.1
- Port-forward → ingress controller
- Ingress rule → expense-api service
- Service → pods


---

## 🔁 Quick TL;DR – Daily Start Up

When coming back to the project on another day (assuming Helm releases and namespaces already exist):

1. Make sure Docker Desktop + Kubernetes are running.
2. Confirm pods are up:

```bash
kubectl get pods -n expense-dev
```

3. If pods are not running, redeploy with Helm:

```bash
cd /Users/marcobiundo/Documents/ExpenseTracker.Api
helm upgrade --install expense-api ./charts/expense-api -n expense-dev
```

4. Port-forward ingress controller:

```bash
kubectl port-forward svc/ingress-nginx-controller 8080:80 -n ingress-nginx
```

5. Open in browser:

```text
http://expense.local:8080/swagger
```

You are now fully back to the Phase 2 Helm + Ingress state.

---

This file is your **reset + startup playbook** for Phase 2.
Read it before Phase 3 to refresh how Helm, Ingress, and local networking are wired together.
