

# Sprint 4.1 — Secrets & Identity Foundation

## Purpose of this Sprint

Sprint 4.1 establishes the **security foundation** for Phase 4. The goal is to ensure that:

- No secrets are stored in code, Helm values, or environment variables
- Azure Key Vault is the single source of truth for secrets
- AKS workloads authenticate to Azure services using **identity**, not credentials
- Access is enforced using **Azure RBAC** and **least privilege**

By the end of this sprint, we prove that a workload running inside AKS can read a secret from Azure Key Vault using **Workload Identity**, with **no secrets involved**.

---

## High-Level Architecture

1. Azure Key Vault is provisioned with RBAC enabled
2. AKS is configured with:
   - System-assigned managed identity
   - OIDC issuer
   - Workload Identity enabled
3. A User Assigned Managed Identity (UAMI) is created for workloads
4. RBAC grants the UAMI read-only access to Key Vault secrets
5. A Kubernetes ServiceAccount is federated with the UAMI
6. A pod authenticates via OIDC and reads a secret from Key Vault

---

## Task 1 — Provision Azure Key Vault (Terraform)

### Goal
Create an Azure Key Vault that:
- Uses Azure RBAC (not access policies)
- Is ready to accept role assignments
- Contains no secrets yet

### Terraform Module: `modules/keyvault/main.tf`

```hcl
resource "azurerm_key_vault" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name

  tenant_id = data.azurerm_client_config.current.tenant_id
  sku_name  = "standard"

  # Critical: Use RBAC instead of access policies
  enable_rbac_authorization = true

  # Dev-friendly settings
  soft_delete_retention_days = 7
  purge_protection_enabled   = false

  tags = var.tags
}

# Retrieves tenant information from the currently authenticated Azure identity
data "azurerm_client_config" "current" {}
```

#### Explanation
- `enable_rbac_authorization = true` ensures all access is governed by Azure RBAC
- No access policies are defined (intentional)
- The vault trusts identities issued by the Entra tenant

### Wiring the Module (env/dev/main.tf)

```hcl
module "keyvault" {
  source = "../../modules/keyvault"

  name                = "expensetracker-dev-kv"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  tags                = local.tags
}
```

### Verification

```bash
az keyvault show \
  --name expensetracker-dev-kv \
  --resource-group rg-expensetracker-network-dev \
  --query properties.enableRbacAuthorization -o tsv
```

Expected output:
```
true
```

---

## Task 2 — Verify AKS Managed Identity

### Goal
Confirm that AKS has a system-assigned managed identity and capture its identifiers.

### Command

```bash
az aks show \
  --name aks-expensetracker-dev \
  --resource-group rg-expensetracker-network-dev \
  --query identity -o json
```

#### Explanation
- AKS uses a system-assigned managed identity by default
- This identity represents the cluster itself
- It will later be used for Azure RBAC role assignments

---

## Task 3 — Grant AKS Access to Key Vault (Terraform)

### Goal
Allow AKS (via identity) to read secrets from Key Vault.

### AKS Module Outputs (modules/aks/outputs.tf)

```hcl
output "aks_principal_id" {
  value = azurerm_kubernetes_cluster.aks.identity[0].principal_id
}
```

### Role Assignment (env/dev/role-assignments.tf)

```hcl
resource "azurerm_role_assignment" "aks_kv_secrets_user" {
  scope                = module.keyvault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = module.aks.aks_principal_id
}
```

#### Explanation
- `Key Vault Secrets User` grants **read-only** access to secrets
- Scope is limited to the Key Vault (least privilege)
- No write or delete permissions are granted

---

## Task 4 — Enable AKS Workload Identity (Terraform)

### Goal
Allow Kubernetes workloads to authenticate to Azure using OIDC tokens.

### AKS Cluster Configuration (modules/aks/main.tf)

```hcl
oidc_issuer_enabled       = true
workload_identity_enabled = true
```

### Verification

```bash
az aks show \
  --name aks-expensetracker-dev \
  --resource-group rg-expensetracker-network-dev \
  --query "oidcIssuerProfile.issuerUrl" -o tsv
```

Expected output: a valid HTTPS URL

#### Explanation
- AKS now exposes an OIDC issuer
- Azure can trust tokens issued for Kubernetes ServiceAccounts

---

## Task 5 — End-to-End Validation (CLI Proof)

> This section is **validation only** and intentionally performed via CLI.
> The pattern will be codified later when wiring the real API.

### Step 1 — Create a Test Secret

```bash
az keyvault secret set \
  --vault-name expensetracker-dev-kv \
  --name "test--kv-read" \
  --value "ok"
```

### Step 2 — Create a User Assigned Managed Identity (UAMI)

```bash
az identity create -g rg-expensetracker-network-dev -n mi-expensetracker-kv-reader-dev
```

Retrieve IDs:

```bash
az identity show -g rg-expensetracker-network-dev -n mi-expensetracker-kv-reader-dev \
  --query "{clientId:clientId, principalId:principalId}" -o table
```

### Step 3 — Grant UAMI Access to Key Vault

```bash
az role assignment create \
  --assignee-object-id <UAMI_PRINCIPAL_ID> \
  --assignee-principal-type ServicePrincipal \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/<sub>/resourceGroups/rg-expensetracker-network-dev/providers/Microsoft.KeyVault/vaults/expensetracker-dev-kv
```

### Step 4 — Create Kubernetes ServiceAccount

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: sa-kv-reader
  namespace: wi-test
  annotations:
    azure.workload.identity/client-id: <UAMI_CLIENT_ID>
```

### Step 5 — Create Federated Credential

```bash
az identity federated-credential create \
  --name fc-sa-kv-reader \
  --identity-name mi-expensetracker-kv-reader-dev \
  --resource-group rg-expensetracker-network-dev \
  --issuer <AKS_OIDC_ISSUER_URL> \
  --subject system:serviceaccount:wi-test:sa-kv-reader \
  --audiences api://AzureADTokenExchange
```

### Step 6 — Test Pod

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: kv-read-test
  namespace: wi-test
  labels:
    azure.workload.identity/use: "true"
spec:
  serviceAccountName: sa-kv-reader
  containers:
  - name: azcli
    image: mcr.microsoft.com/azure-cli
    command: ["/bin/sh","-c"]
    args:
      - |
        TOKEN=$(cat $AZURE_FEDERATED_TOKEN_FILE)
        az login --service-principal \
          --client-id $AZURE_CLIENT_ID \
          --tenant $AZURE_TENANT_ID \
          --federated-token "$TOKEN" >/dev/null
        az keyvault secret show \
          --vault-name expensetracker-dev-kv \
          --name test--kv-read \
          --query value -o tsv
```

### Expected Output
```
ok
```

---

## Sprint 4.1 Exit Criteria — Met

- Key Vault provisioned with RBAC
- AKS identity verified
- Workload Identity enabled
- Least-privilege access enforced
- Secrets retrieved using identity only
- No secrets stored in code or configuration

Sprint 4.1 is complete and provides a secure foundation for Phase 4.