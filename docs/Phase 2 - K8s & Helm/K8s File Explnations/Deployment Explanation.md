<file name=0 path=/Users/marcobiundo/Documents/ExpenseTracker/ExpenseTracker.Api/Documentation/Phase 2 - K8s & Helm/K8s File Explnations/Deployment Explanation.md># Kubernetes Deployment – Expense API (Line‑by‑Line Explanation)

This document explains the `expense-api-deployment.yaml` file line by line so that I can understand **what every section does** and eventually write a similar Deployment from scratch without copying.

---

## Full YAML for Reference

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: expense-api
  labels:
    app: expense-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: expense-api
  template:
    metadata:
      labels:
        app: expense-api
    spec:
      containers:
        - name: expense-api
          image: marcobiundo/expense-api:v2
          imagePullPolicy: Always
          ports:
            - containerPort: 8080
          envFrom:
            - configMapRef:
                name: expense-api-config
            - secretRef:
                name: expense-api-secrets
          resources:
            requests:
              cpu: "100m"
              memory: "256Mi"
            limits:
              cpu: "500m"
              memory: "512Mi"
          readinessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 20
```

---

## Top‑Level Metadata

### `apiVersion: apps/v1`

- **What it is:** The version of the Kubernetes API this object uses.
- **Why:** Different resource types live in different API groups and versions.
- **Here:** `apps/v1` is the current stable API group for Deployments.

If I change this to the wrong value, Kubernetes will reject the manifest or treat it as a different kind of object.

---

### `kind: Deployment`

- **What it is:** The type of Kubernetes object I’m defining.
- **Here:** A `Deployment` manages a set of Pods and handles updates and rollbacks.
- **Mental model:**  
  A Deployment is the “controller” that keeps `N` copies of my app running and replaces them during upgrades.

---

### `metadata:`

```yaml
metadata:
  name: expense-api
  labels:
    app: expense-api
```

- **`name`:**  
  The unique name of this Deployment in the `expense-dev` namespace.  
  I use this name when I run commands like:

  ```bash
  kubectl get deployment expense-api
  kubectl describe deployment expense-api
  ```

- **`labels`:**  
  Key/value pairs that describe this object. They’re used for:
  - Grouping
  - Selecting objects
  - Filtering in `kubectl` commands or tools

Here, I use a simple label:

```yaml
app: expense-api
```

This same label is reused in the Pod template and the Service selector so everything ties together.

---

## `spec` – Desired State of the Deployment

```yaml
spec:
  replicas: 2
  selector:
    matchLabels:
      app: expense-api
  template:
    ...
```

The `spec` tells Kubernetes **what I want**: how many Pods, how to find them, and what each Pod should look like.

---

### `replicas: 2`

- **What it means:**  
  I want Kubernetes to keep **2 Pods** of this app running.
- **Why:**  
  - Basic redundancy: if one pod dies, I still have another.
  - Better for rolling updates and future scaling.

If I change this to 3 and apply again, the Deployment will create a third Pod automatically.

---

### `selector:`

```yaml
selector:
  matchLabels:
    app: expense-api
```

- **What it is:**  
  This tells the Deployment **which Pods belong to it**.
- **Key rule:**  
  The labels on the Pods (defined in the template) **must match** this selector. If they don’t match, the Deployment won’t manage those Pods.

Here:
- `matchLabels.app = expense-api`
- So any Pod with the label `app: expense-api` is considered part of this Deployment.

---

## `template` – Pod Template

```yaml
template:
  metadata:
    labels:
      app: expense-api
  spec:
    containers:
      - name: expense-api
        ...
```

The `template` describes **what each Pod looks like**.

### `template.metadata.labels`

```yaml
metadata:
  labels:
    app: expense-api
```

- These are the labels applied to **Pods created by this Deployment**.
- Must match the `selector.matchLabels` above.
- Also used by the Service to route traffic.

---

## Pod `spec` – Container Details

```yaml
spec:
  containers:
    - name: expense-api
      image: marcobiundo/expense-api:v2
      imagePullPolicy: Always
      ports:
        - containerPort: 8080
      envFrom:
        - configMapRef:
            name: expense-api-config
        - secretRef:
            name: expense-api-secrets
      resources:
        ...
      readinessProbe:
        ...
      livenessProbe:
        ...
```

This is where I describe the **container** that runs inside each Pod.

---

### `containers:` and `name: expense-api`

```yaml
containers:
  - name: expense-api
```

- `containers:` is a **list** of containers in the Pod.
- Here, I only have one container.
- `name` is an internal name used in logs and commands like:

  ```bash
  kubectl logs deployment/expense-api -c expense-api
  ```

---

### `image: marcobiundo/expense-api:v2`

- **What it is:**  
  The Docker image the container will run.
- **Structure:**  
  `<docker-username>/<repository>:<tag>`
- **Here:**  
  This image is stored in Docker Hub under my account and tag `v2`.

When I build and push a new version, I can:
- Update this tag (e.g., `v3`)
- Or re‑use `latest` if I want to always pull the newest (not ideal for production).

---

### `imagePullPolicy: Always`

- **What it does:**  
  Tells Kubernetes **when** to pull the image:
  - `Always` → Pull every time the Pod is created.
  - `IfNotPresent` → Use local cache if available.
  - `Never` → Never pull (only use cached images).

Here, `Always` ensures:
- The cluster always fetches the latest version of `v2` from the registry.
- Good for development while I’m iterating.

---

### `ports:`

```yaml
ports:
  - containerPort: 8080
```

- **What it is:**  
  The port that the container is listening on **inside** the Pod.
- **Important:**  
  This must match the port the ASP.NET Core app is listening on (e.g., `ASPNETCORE_URLS=http://+:8080` or `Kestrel` config).
- Used by:
  - Probes (`readinessProbe`, `livenessProbe`)
  - Services (`targetPort`)

---

### `envFrom:` – Environment Variables from ConfigMaps and Secrets

```yaml
envFrom:
  - configMapRef:
      name: expense-api-config
  - secretRef:
      name: expense-api-secrets
```

Instead of defining environment variables directly in the Deployment, I now reference external Kubernetes resources:

- `configMapRef` points to a ConfigMap named `expense-api-config`.
- `secretRef` points to a Secret named `expense-api-secrets`.

Kubernetes will inject all key/value pairs from these resources as environment variables into the container.

This approach helps me:

- Separate configuration from code.
- Manage sensitive data securely (Secrets).
- Update configuration without changing the Deployment manifest.

**How ASP.NET Core uses this:**

ASP.NET Core maps environment variables with double underscores (`__`) to nested configuration keys. For example:

- An environment variable named `ConnectionStrings__DefaultConnection` maps to the JSON path `ConnectionStrings:DefaultConnection`.
- This allows complex configuration structures to be represented as flat environment variables.

---

### `resources:` – Requests and Limits

```yaml
resources:
  requests:
    cpu: "100m"
    memory: "256Mi"
  limits:
    cpu: "500m"
    memory: "512Mi"
```

- **What this does:**  
  Sets resource **requests** and **limits** for the container.

- `requests`:
  - Minimum resources the container is **guaranteed**.
  - Used by the scheduler to decide on which node to place the Pod.
  - Here:
    - CPU: `100m` (0.1 core)
    - Memory: `256Mi` ~ 256 MB

- `limits`:
  - The maximum the container is allowed to use.
  - If it uses more CPU → it may be throttled.
  - If it uses more memory → it may be OOM‑killed.
  - Here:
    - CPU: `500m` (0.5 core)
    - Memory: `512Mi` ~ 512 MB

These are **starting values** that I can adjust as I measure real usage.

---

## Probes – Health Checks for Kubernetes

Kubernetes doesn’t just trust that my container is healthy.  
It regularly calls endpoints I specify to check **readiness** and **liveness**.

These probes use the `/health` endpoint I mapped in `Program.cs`.

---

### `readinessProbe`

```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
```

- **Purpose:**  
  Tells Kubernetes **when the app is ready to receive traffic**.

- **`httpGet`:**
  - K8s calls `http://<pod-ip>:8080/health`.

- **`initialDelaySeconds: 10`:**
  - Wait 10 seconds after the container starts before starting readiness checks.
  - This gives the app time to start up.

- **`periodSeconds: 10`:**
  - Check readiness every 10 seconds.

If the readiness probe fails:
- Kubernetes will **NOT** send traffic to this Pod via the Service.
- The Pod can still be “Running” but not “Ready”.

This prevents traffic from going to a half‑initialized or broken instance.

---

### `livenessProbe`

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 20
```

- **Purpose:**  
  Tells Kubernetes if the container is still alive and healthy over time.

- **Key idea:**
  - If this probe **keeps failing**, Kubernetes assumes the container is stuck and **restarts it**.

- **Settings:**
  - `initialDelaySeconds: 30` → Give the app more time before liveness starts.
  - `periodSeconds: 20` → Check every 20 seconds.

It’s okay for readiness and liveness to both hit `/health` for now.  
Later, I can differentiate them if needed (e.g., `/health/ready` vs `/health/live`).

---

## Configuration Moved to ConfigMaps and Secrets

Previously, environment variables like `ASPNETCORE_ENVIRONMENT` and `RunMigrations` were defined directly inside the Deployment manifest.

Now, these configuration values have been moved out of the Deployment and into Kubernetes ConfigMaps and Secrets:

- The ConfigMap `expense-api-config` holds non-sensitive configuration data.
- The Secret `expense-api-secrets` holds sensitive data like passwords or API keys.

The Deployment references these external resources via `envFrom`, which injects their key/value pairs as environment variables into the container.

This separation allows:

- Easier configuration management and updates without redeploying the application.
- Secure handling of sensitive information.
- Reuse of configurations across multiple Deployments or environments.

---

## How All of This Fits Together

1. **Deployment**:
   - Ensures I always have `replicas: 2` Pods running.
   - Uses a Pod template that runs `marcobiundo/expense-api:v2`.

2. **Selector + Labels**:
   - `selector.matchLabels.app = expense-api`
   - Pod template adds `labels.app = expense-api`
   - This ties the Deployment to its Pods and will also let a Service discover them.

3. **Container Configuration**:
   - Runs ASP.NET Core on port 8080.
   - Uses environment variables injected from ConfigMaps and Secrets for environment and migrations behavior.
   - Has basic CPU/memory hints.

4. **Probes**:
   - Call `/health` to decide when to mark the Pod as Ready and when to restart it if it gets sick.

Once I understand these building blocks, I can:
- Change the image tag when I ship new versions.
- Adjust replicas for scaling.
- Tune resource limits.
- Break out env variables into ConfigMaps and Secrets.
- Reuse this pattern for other microservices.

The key patterns to remember:

- **Deployment = desired state and rollout controller**
- **Labels + selectors = how Kubernetes wires resources together**
- **Probes = how Kubernetes decides when to trust or restart my app**
- **Resources = CPU/memory contract with the cluster**
- **ConfigMaps and Secrets = externalized configuration management**
