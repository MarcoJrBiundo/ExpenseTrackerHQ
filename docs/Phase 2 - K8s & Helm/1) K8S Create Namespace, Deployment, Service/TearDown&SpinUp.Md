## Tear Down

```bash
CTRL+C (stop port-forward)
kubectl delete -f k8s/expense-api-service.yaml
kubectl delete -f k8s/expense-api-deployment.yaml
minikube stop
```

## Spin Up
```bash
minikube start
kubectl create namespace expense-dev   # only if deleted
kubectl config set-context --current --namespace=expense-dev
kubectl apply -f k8s/expense-api-deployment.yaml
kubectl apply -f k8s/expense-api-service.yaml
kubectl port-forward svc/expense-api 5000:80
```