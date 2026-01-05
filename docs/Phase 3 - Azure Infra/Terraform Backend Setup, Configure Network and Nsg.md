

# Sprint 3.1 — Terraform Backend, Networking, and NSGs (Reusable Playbook)

This document captures a repeatable, production-style approach to bootstrapping Azure infrastructure using Terraform:

1. **Create a remote Terraform backend** (Azure Storage) to store state safely.
2. **Initialize Terraform** with provider/version pinning.
3. **Provision core networking** (Resource Group, VNet, Subnets).
4. **Attach NSGs** to subnets to establish a security boundary.

The intent is to make this guide usable for future projects with minimal changes.

---

## Why This Sprint Exists

Terraform needs a place to store *state* — the source of truth for what it has created. A remote backend enables:

- Team collaboration (shared state instead of local files)
- Safe locking (prevents two applies at once)
- Disaster recovery (state isn’t tied to one laptop)
- CI/CD compatibility (pipelines can run Terraform safely)

Networking is built early because almost every Azure resource depends on it:
AKS needs a subnet, private endpoints need subnets, and network boundaries drive security decisions.

---

## Repository Layout Assumption (Monorepo-Friendly)

From the repository root (`ExpenseTrackerHQ`):

```text
infra/
  terraform/
    modules/
      networking/
    env/
      dev/
```

**Why this layout:**
- `modules/` contains reusable building blocks (networking, aks, acr, sql, etc.).
- `env/dev` contains environment-specific wiring and naming.
- This scales cleanly to `env/test` and `env/prod` without duplicating module logic.

---

## Step 1 — Create an Azure Subscription

Create (or select) the Azure subscription that will own these resources.

**Why:**
- The subscription is your billing and governance boundary.
- It keeps environments separate and makes cleanup predictable.

---

## Step 2 — Manually Create the Terraform Backend (One-Time Bootstrap)

Terraform cannot create its own backend *until a backend exists*. So I bootstraped the backend manually once.

Create these resources in Azure (Portal or CLI):

- **Resource Group** (example): `rg-tfstate-dev`
- **Storage Account** (example): `tfstate<unique>`
  - Standard / LRS is usually sufficient for dev
- **Blob Container** (example): `tfstate`
  - Private access

**Why this is done manually:**
- Terraform must read/write state before it can manage anything else.
- After this step, all subsequent infrastructure can be IaC-driven.

---

## Step 3 — Configure the Terraform Backend Block

In `infra/terraform/env/dev/backend.tf`:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-tfstate-dev"
    storage_account_name = "tfstate<unique>"
    container_name       = "tfstate"
    key                  = "dev/phase3.tfstate"
  }
}
```

**Why the `key` looks like `dev/phase3.tfstate`:**
- The key is the *path/name of the state file inside the container*.
- Prefixing with `dev/` prevents future environments (test/prod) from accidentally sharing state.
- Keeping it phase-scoped makes it obvious what the state controls.

---

## Step 4 — Pin Terraform and Provider Versions

In `infra/terraform/env/dev/versions.tf`:

```hcl
terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}
```

**Why this matters:**
- Prevents “works on my machine” drift.
- Ensures consistent behavior across laptops and CI.
- Makes upgrades deliberate instead of accidental.

---

## Step 5 — Configure the Azure Provider

In `infra/terraform/env/dev/providers.tf`:

```hcl
provider "azurerm" {
  features {}
}
```

**Why:**
- `azurerm` is the official Azure provider.
- The `features {}` block is required and enables provider feature toggles.

---

## Step 6 — Authenticate to Azure and Select the Subscription

```bash
az login
az account set --subscription "<YOUR_SUBSCRIPTION_ID_OR_NAME>"
```

**Why:**
- Terraform uses Azure authentication (via CLI or env vars) to call Azure APIs.
- Setting the subscription ensures Terraform deploys into the correct place.

---

## Step 7 — Initialize Terraform (Backend + Providers + Modules)

From `infra/terraform/env/dev`:

```bash
terraform init
```

**What this does:**
- Connects to the remote backend
- Downloads provider plugins
- Prepares the working directory for plan/apply
- Pulls module source code (when modules exist)

**Why init is always first:**
- Terraform can’t plan/apply until providers/modules/backend are ready.

---

## Step 8 — Create a Reusable Networking Module

Create the module:

- `infra/terraform/modules/networking/main.tf`
- `infra/terraform/modules/networking/variables.tf`
- `infra/terraform/modules/networking/outputs.tf`

### Module: main.tf (VNet + Subnets)
```hcl
resource "azurerm_resource_group" "network" {
  name     = var.rg_name
  location = var.location
}

resource "azurerm_virtual_network" "vnet" {
  name                = var.vnet_name
  address_space       = [var.vnet_cidr]
  location            = azurerm_resource_group.network.location
  resource_group_name = azurerm_resource_group.network.name
}

resource "azurerm_subnet" "aks" {
  name                 = var.aks_subnet_name
  resource_group_name  = azurerm_resource_group.network.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = [var.aks_subnet_cidr]
}

resource "azurerm_subnet" "sql" {
  name                 = var.sql_subnet_name
  resource_group_name  = azurerm_resource_group.network.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = [var.sql_subnet_cidr]
}
```

### Module: variables.tf
```hcl
variable "rg_name"         { type = string }
variable "location"        { type = string }

variable "vnet_name"       { type = string }
variable "vnet_cidr"       { type = string }

variable "aks_subnet_name" { type = string }
variable "aks_subnet_cidr" { type = string }

variable "sql_subnet_name" { type = string }
variable "sql_subnet_cidr" { type = string }

variable "aks_nsg_name"    { type = string }
variable "sql_nsg_name"    { type = string }
```

### Module: outputs.tf
```hcl
output "vnet_id"       { value = azurerm_virtual_network.vnet.id }
output "aks_subnet_id" { value = azurerm_subnet.aks.id }
output "sql_subnet_id" { value = azurerm_subnet.sql.id }
output "rg_name"       { value = azurerm_resource_group.network.name }
output "location"      { value = azurerm_resource_group.network.location }
```

**Why I use a module:**
- Encapsulates networking as a reusable unit.
- Avoids duplication across environments.
- Makes the environment layer a simple “configuration file” (names/CIDRs).

---

## Step 9 — Add NSGs and Associate Them to Subnets

Append to `modules/networking/main.tf`:

```hcl
resource "azurerm_network_security_group" "aks" {
  name                = var.aks_nsg_name
  location            = azurerm_resource_group.network.location
  resource_group_name = azurerm_resource_group.network.name
}

resource "azurerm_network_security_group" "sql" {
  name                = var.sql_nsg_name
  location            = azurerm_resource_group.network.location
  resource_group_name = azurerm_resource_group.network.name
}

resource "azurerm_subnet_network_security_group_association" "aks" {
  subnet_id                 = azurerm_subnet.aks.id
  network_security_group_id = azurerm_network_security_group.aks.id
}

resource "azurerm_subnet_network_security_group_association" "sql" {
  subnet_id                 = azurerm_subnet.sql.id
  network_security_group_id = azurerm_network_security_group.sql.id
}
```

**Why attach NSGs now (even with minimal rules):**
- NSGs define a security boundary at the subnet level.
- It’s easier to tighten security later if the boundary exists early.
- AKS and Private Endpoints often require thoughtful network control; starting with explicit NSGs keeps the design intentional.

**Rule strategy (for early phases):**
- Keep NSG rules minimal while you bring up core services.
- Tighten inbound/outbound rules once AKS + SQL + Private Endpoints are validated.

---

## Step 10 — Wire the Module into the Dev Environment

In `infra/terraform/env/dev/main.tf`:

```hcl
module "networking" {
  source   = "../../modules/networking"

  rg_name  = "rg-expensetracker-network-dev"
  location = "canadacentral"

  vnet_name = "vnet-expensetracker-dev"
  vnet_cidr = "10.0.0.0/16"

  aks_subnet_name = "snet-aks"
  aks_subnet_cidr = "10.0.1.0/24"

  sql_subnet_name = "snet-sql"
  sql_subnet_cidr = "10.0.2.0/24"

  aks_nsg_name = "nsg-expensetracker-aks-dev"
  sql_nsg_name = "nsg-expensetracker-sql-dev"
}
```

**Why `env/dev` exists:**
- It’s a thin layer that selects names, regions, and CIDRs.
- This is how you scale to `env/test` and `env/prod` without rewriting modules.

---

## Step 11 — Format, Validate, Plan, Apply

From `infra/terraform/env/dev`:

```bash
terraform fmt -recursive
terraform validate
terraform plan
terraform apply
```

**Why this command order:**
- `fmt` keeps code consistent and diff-friendly.
- `validate` catches structural/config issues early.
- `plan` previews changes (your safety net).
- `apply` executes the plan and updates remote state.

---

## Step 12 — Verify in Azure (Optional but Recommended)

Validate that resources exist and are associated properly:

- Resource group created
- VNet + subnets created
- NSGs created
- NSGs associated to the correct subnets

**Why verify in Portal:**
- Helps you build the mental map of Azure resources.
- Makes demos easier (“here’s the VNet, here are subnets, here are NSGs”).

---

## Outputs You Should Expect

At the end of Sprint 3.1, you should have:

- A remote Terraform backend storing state in Azure Storage
- A networking RG (dev)
- A VNet with two subnets (`snet-aks`, `snet-sql`)
- Two NSGs attached to those subnets
- Outputs available for downstream modules (AKS, SQL, Private Endpoints)

---

## Next Steps (Sprint 3.2 Preview)

Typical next infrastructure increments:

- **ACR** (Container Registry)
- **AKS** (Cluster + node pools + VNet integration)
- **Azure SQL** (Server + DB)
- **Private Endpoint + Private DNS Zone** (secure SQL connectivity)
- **Managed Identity** (secure auth, minimize secrets)

---