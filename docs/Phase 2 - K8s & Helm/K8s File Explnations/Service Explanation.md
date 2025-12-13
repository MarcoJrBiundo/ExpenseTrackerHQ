 
# Kubernetes Service – Expense API (Line‑by‑Line Explanation)

This document explains the `expense-api-service.yaml` file line by line so I fully understand **what each part means**, why it exists, and how to rebuild a Service manifest myself from scratch.

---

## Full YAML for Reference

```yaml
apiVersion: v1
kind: Service
metadata:
  name: expense-api
  labels:
    app: expense-api
spec:
  type: ClusterIP
  selector:
    app: expense-api
  ports:
    - name: http
      port: 80
      targetPort: 8080
```

---

## `apiVersion: v1`

- **What it is:** The version of the Kubernetes API used to create this object.
- **Why:** Services are part of the **core API group**, which lives under `v1`.
- This is the most stable and long‑lived API version in Kubernetes.

---

## `kind: Service`

- **What it is:** The type of Kubernetes resource we're defining.
- A **Service** provides a **stable, fixed IP + DNS name** that forwards traffic to one or more Pods.
- Without a Service, Pods only have their own internal IPs, which change on every restart.

Think of a Service as a **load balancer inside the cluster**.

---

## `metadata`

```yaml
metadata:
  name: expense-api
  labels:
    app: expense-api
```

### `name: expense-api`

- The unique name of the Service.
- Used in commands like:

  ```bash
  kubectl get svc expense-api
  kubectl describe svc expense-api
  ```

### `labels`

- Key/value identifiers attached to this Service.
- They help with organization, filtering, and tooling.
- These labels do **not** affect routing — the selector does.

Reusing the label `app: expense-api` keeps everything consistent across Deployment + Pods + Service.

---

## `spec` – How the Service Behaves

```yaml
spec:
  type: ClusterIP
  selector:
    app: expense-api
  ports:
    - name: http
      port: 80
      targetPort: 8080
```

The `spec` defines:

- The **type** of Service.
- Which Pods it sends traffic to (`selector`).
- What ports it exposes (`ports`).

---

## `type: ClusterIP`

- This is the **default Kubernetes Service type**.
- Provides an internal‑only IP accessible **inside the cluster**.
- Does **not** expose the app to your laptop or the outside world.
- Good for:
  - Pod‑to‑Pod traffic
  - API → DB communication
  - Ingress controllers (they front a ClusterIP service)

If I want external access later:
- `NodePort` exposes it on each node
- `LoadBalancer` allocates a cloud load balancer
- `Ingress` becomes the cleanest option

But for local K8s development, `ClusterIP` is perfect.

---

## `selector`

```yaml
selector:
  app: expense-api
```

- The selector is the **heart** of Kubernetes networking.
- It tells the Service *which Pods to send traffic to*.
- Kubernetes creates an **endpoints list** behind the scenes.

This Service will route traffic to any Pod with:

```yaml
labels:
  app: expense-api
```

These are the exact labels we applied in the Deployment’s Pod template — this is how everything connects.

If labels don't match:
- Service sends traffic **nowhere**
- Port‑forward still works because it bypasses the cluster, but internal routing fails

---

## `ports` block

```yaml
ports:
  - name: http
    port: 80
    targetPort: 8080
```

This defines how traffic flows from the Service → Pod.

### `name: http`

- A human‑readable name for the port.
- Helpful in tools like:
  - Istio
  - Linkerd
  - Ingress controllers
- Optional but recommended.

### `port: 80`

- The internal port exposed by the Service **inside the cluster**.
- Other apps in the cluster talk to the Service at:
  
  ```
  http://expense-api:80
  ```

- It does **not** have to match the container port.

### `targetPort: 8080`

- This is the port inside the container.
- It must match the `containerPort` defined in the Deployment.

In your API Dockerfile, ASP.NET Core listens on **8080**, so:

- Traffic hits Service at **port 80**
- Service forwards to Pod → containerPort **8080**

This separation lets you choose clean cluster ports while keeping container ports unchanged.

---

## How the Service Works Internally (Mental Model)

1. **Service gets a stable IP** inside the cluster.
2. **Selector finds all matching Pods** (`app: expense-api`).
3. Kubernetes builds a list of Pod IPs called **Endpoints**.
4. When something calls:

   ```
   http://expense-api:80
   ```

   Kubernetes load‑balances traffic between:

   ```
   <pod-ip-1>:8080
   <pod-ip-2>:8080
   ```

5. If a Pod dies:
   - Deployment recreates it
   - New Pod IP is added to the Service endpoints
   - Traffic continues uninterrupted

This is what makes Services so powerful — they abstract away Pod churn.

---

## Testing the Service Locally

Because `ClusterIP` does NOT expose anything outside the cluster, we use:

```bash
kubectl port-forward svc/expense-api 5000:80
```

This means:

- Your laptop hits `http://localhost:5000`
- Kubernetes forwards that to the **Service**
- Service forwards to **Pods**

Then:

- `http://localhost:5000/health`
- `http://localhost:5000/swagger`

Both confirm:

- Pod is Running
- Probes are passing
- Service is routing correctly

---

## After Mastering This

Once this makes sense, I can easily write:

- NodePort Services
- LoadBalancer Services (for cloud)
- Multi-port Services
- Headless Services (`clusterIP: None`)
- Services for databases
- Services consumed by Ingress

Understanding this single YAML unlocks all of that.

---

## Summary (What I Should Remember)

- **Service = stable internal load balancer for Pods**
- `ClusterIP` = internal-only access
- `selector` connects Service → Pod labels
- `port` = Service port  
- `targetPort` = container port
- Without a Service, Pods are not reachable reliably
- This is a fundamental building block for Kubernetes apps