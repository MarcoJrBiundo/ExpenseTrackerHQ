## K8s Local Doc

## Installed Kubectl and ensure install with 
```bash
kubectl version --client
```

## Install MiniKube and ensure install with 
```bash
minikube version
```

## Check if Docker Desktop Kubernetes is installed/enabled
```bash
kubectl config get-contexts
```

## Start MiniKub

```bash
minikube start
```


## Check Nodes

```bash
kubectl get nodes
```

## Create Namespace 

```bash
kubectl create namespace example-dev
```


## Set example-dev as the default namespace for this context

```bash
kubectl config set-context --current --namespace=example-dev
```


## Confirm Namespace with 

```sh
kubectl config get-contexts
```

you should see 

```text
CURRENT   NAME       CLUSTER    AUTHINFO   NAMESPACE
*         minikube   minikube   minikube   example-dev
```


## Quick health check

```sh
kubectl get pods
```

you should see 

```text
No resources found in example-dev namespace.
```

## Create Deployment Yaml
in the root of your Solution ( where .sln Lives) create a k8s directory and a example-api-deployment.yaml

## Deployment Example 
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: example-api
  labels:
    app: example-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: example-api
  template:
    metadata:
      labels:
        app: example-api
    spec:
      containers:
        - name: example-api
          # TODO: replace this with the image you built in Phase 1
          image: marcobiundo/example-api:v2
          imagePullPolicy: IfNotPresent
          ports:
            - containerPort: 8080
          env:
            # We'll later move these to ConfigMap / Secret
            - name: ASPNETCORE_ENVIRONMENT
              value: "Development"
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

## Apply Deployment and Verify Pods

From the root of your solution (where the `.sln` lives), run a dry run first to validate the YAML:

```bash
kubectl apply -f k8s/example-api-deployment.yaml --dry-run=client
```

If the dry run succeeds, apply it for real:

```bash
kubectl apply -f k8s/example-api-deployment.yaml
```

Check the pods in the `example-dev` namespace:

```bash
kubectl get pods
```

You should see something like:

```text
NAME                           READY   STATUS    RESTARTS   AGE
example-api-7d7b5fb58d-8vc55   1/1     Running   0          1m
example-api-7d7b5fb58d-zgppx   1/1     Running   0          1m
```

- `READY 1/1` means the single container in each pod is healthy.
- `STATUS Running` means the pod is up.
- If the probes are misconfigured, you'll see `0/1` or `CrashLoopBackOff` instead, which is a signal to check `kubectl describe pod` and `kubectl logs`.

## Create Service YAML

To expose the deployment inside the cluster, create a Service definition file at `k8s/example-api-service.yaml`:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: example-api
  labels:
    app: example-api
spec:
  type: ClusterIP
  selector:
    app: example-api
  ports:
    - name: http
      port: 80          # Service port inside the cluster
      targetPort: 8080  # Container port defined in the Deployment
```

Apply the Service:

```bash
kubectl apply -f k8s/example-api-service.yaml
kubectl get svc
```

You should see something like:

```text
NAME          TYPE        CLUSTER-IP     PORT(S)   AGE
example-api   ClusterIP   10.x.x.x       80/TCP    10s
```

## Port-Forward and Test API from Local Machine

To hit the API from your laptop, forward a local port to the Service:

```bash
kubectl port-forward svc/example-api 5000:80
```

Now you can access:

- Health endpoint:
  ```text
  http://localhost:5000/health
  ```
- Swagger UI:
  ```text
  http://localhost:5000/swagger
  ```

If both work, it confirms that:

- The Deployment is running correctly.
- The Service routes traffic to the pods.
- The probes on `/health` are correctly configured and the app is reachable.