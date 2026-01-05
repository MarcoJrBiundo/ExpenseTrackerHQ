# Sprint 3.4 — Azure SQL + Private Connectivity (Deep Dive)

---

## Overview

Sprint 3.4 is a critical milestone in **Phase 3 — Azure Infrastructure**, focusing on provisioning Azure SQL resources and establishing secure, private connectivity from our AKS cluster. This sprint lays the foundational infrastructure for a secure, scalable backend database solution, but deliberately stops short of integrating the database with the application layer. The goal is to ensure robust Azure SQL deployment and private network connectivity, setting the stage for Phase 4 where secrets management and managed identity will enable actual database usage.

---

## Goal of Sprint 3.4

- Provision an **Azure SQL Server** and **Azure SQL Database** instance.
- Create a **Private Endpoint** for the SQL server to enable private network access.
- Configure a **Private DNS Zone** and link it to the Virtual Network (VNet) for seamless DNS resolution.
- Validate connectivity from the AKS cluster to the Azure SQL instance over the private endpoint.
- Establish outputs and documentation to support future integration and troubleshooting.

This sprint ensures infrastructure readiness and secure networking without yet exposing the SQL server publicly or integrating it with the API.

---

## Step-by-Step Execution

### 1. Azure SQL Server & Database Provisioning

I began by defining Terraform resources for the Azure SQL Server and Database.

```hcl
resource "azurerm_mssql_server" "main" {
  name                         = "expense-sqlsrv"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password

  identity {
    type = "SystemAssigned"
  }
  
  tags = var.tags
}
```

**Line-by-line explanation:**

- `name`: The globally unique name for the SQL Server instance.
- `resource_group_name`: Associates the server with the existing resource group.
- `location`: Ensures the server is deployed in the same Azure region as other resources.
- `version`: Specifies the SQL Server version; "12.0" corresponds to Azure SQL Database engine.
- `administrator_login` & `administrator_login_password`: Credentials for server admin access, sourced securely from variables.
- `identity`: Enables a system-assigned managed identity for potential future use (e.g., Key Vault access).
- `tags`: Organizational metadata for cost tracking and management.

Next, the database resource:

```hcl
resource "azurerm_mssql_database" "main" {
  name                = "expense-db"
  server_id           = azurerm_mssql_server.main.id
  sku_name            = "Basic"
  max_size_gb         = 2
  collation           = "SQL_Latin1_General_CP1_CI_AS"
  zone_redundant      = false
  read_scale          = false
  auto_pause_delay_in_minutes = 60
}
```

**Line-by-line explanation:**

- `name`: The database name within the SQL server.
- `server_id`: Links this database to the SQL Server resource.
- `sku_name`: Selects the pricing tier; "Basic" is cost-effective for dev/test.
- `max_size_gb`: Maximum database size allowed.
- `collation`: Sets the character set and sorting rules.
- `zone_redundant`: Disabled here to reduce cost and complexity.
- `read_scale`: Disabled; no read replicas needed at this stage.
- `auto_pause_delay_in_minutes`: Enables auto-pausing to save cost during inactivity.

---

### 2. Private Endpoint Creation

To secure connectivity, I created a Private Endpoint to the SQL Server:

```hcl
resource "azurerm_private_endpoint" "sql" {
  name                = "sql-pe"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  subnet_id           = azurerm_subnet.aks_subnet.id

  private_service_connection {
    name                           = "sql-psc"
    private_connection_resource_id = azurerm_mssql_server.main.id
    is_manual_connection           = false
    subresource_names              = ["sqlServer"]
  }
}
```

**Line-by-line explanation:**

- `name`: Identifier for the private endpoint resource.
- `location` & `resource_group_name`: Aligns with existing infrastructure.
- `subnet_id`: The subnet within the VNet where the endpoint will reside, here the AKS subnet to allow cluster access.
- `private_service_connection`: 
  - `name`: Name of the connection.
  - `private_connection_resource_id`: Links the endpoint to the SQL Server resource.
  - `is_manual_connection`: Set to false to allow automatic approval.
  - `subresource_names`: Specifies the SQL Server subresource exposed via the endpoint.

---

### 3. Private DNS Zone Setup and Linking

Azure SQL private endpoints require DNS resolution to the private IP address. I set up a Private DNS Zone and link it to our VNet:

```hcl
resource "azurerm_private_dns_zone" "sql" {
  name                = "privatelink.database.windows.net"
  resource_group_name = azurerm_resource_group.main.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "sql_vnet_link" {
  name                  = "sql-dns-link"
  resource_group_name   = azurerm_resource_group.main.name
  private_dns_zone_name = azurerm_private_dns_zone.sql.name
  virtual_network_id    = azurerm_virtual_network.main.id
  registration_enabled  = false
}

resource "azurerm_private_dns_a_record" "sql" {
  name                = azurerm_mssql_server.main.name
  zone_name           = azurerm_private_dns_zone.sql.name
  resource_group_name = azurerm_resource_group.main.name
  ttl                 = 300
  records             = [azurerm_private_endpoint.sql.private_service_connection[0].private_ip_address]
}
```

**Line-by-line explanation:**

- `azurerm_private_dns_zone`:
  - `name`: The DNS zone used by Azure SQL's private link.
  - `resource_group_name`: Where the DNS zone is created.
- `azurerm_private_dns_zone_virtual_network_link`:
  - Links the DNS zone to the VNet so that resources inside can resolve private endpoints.
  - `registration_enabled` is false since I do not want auto-registration of VMs or other resources.
- `azurerm_private_dns_a_record`:
  - Creates an A record pointing the SQL Server FQDN to the private IP of the endpoint.
  - `ttl`: Time to live for DNS caching.
  - `records`: Contains the private IP address assigned to the private endpoint.

---

### 4. Validation of AKS → SQL Private Connectivity

Once the infrastructure is deployed, I validate connectivity from the AKS cluster.

**Step 1:** Connect to the AKS cluster:

```bash
az aks get-credentials --resource-group myResourceGroup --name myAKSCluster
```

- `--resource-group`: The resource group containing the AKS cluster.
- `--name`: The AKS cluster name.
- This command configures `kubectl` to communicate with the AKS cluster.

**Step 2:** Launch a debug pod with SQL client tools:

```bash
kubectl run sqlclient --image=mcr.microsoft.com/mssql-tools --restart=Never --rm -it -- bash
```

- `run sqlclient`: Creates a pod named `sqlclient`.
- `--image`: Specifies the container image with SQL tools.
- `--restart=Never`: Run the pod as a one-off job.
- `--rm`: Remove pod after exit.
- `-it`: Interactive terminal.
- `-- bash`: Run bash shell inside the container.

**Step 3:** Inside the pod, test SQL connectivity:

```bash
sqlcmd -S expense-sqlsrv.database.windows.net -U <admin_user> -P <admin_password> -Q "SELECT @@VERSION"
```

- `-S`: The SQL Server address (should resolve to private IP via DNS).
- `-U` & `-P`: Admin login credentials.
- `-Q`: Runs the query and exits.

If the command returns SQL Server version info, private connectivity is confirmed.

---

## Why SQL Is Not Yet Accessed by the API

- This sprint focuses solely on **infrastructure provisioning and network connectivity**.
- The API does not yet have **secrets management** or **managed identity** integration to securely authenticate to SQL.
- Actual database usage, schema migrations, and application integration are deferred to **Phase 4**, where I will implement **Azure Key Vault** and **Managed Identity** for secure credential handling.
- This staged approach minimizes risk and isolates infrastructure concerns from application logic.

---

## Cost Awareness

- **Azure SQL Basic / DTU model** was chosen because:
  - It is **cost-effective** for development and testing environments.
  - Provides sufficient performance for initial validation without incurring high costs.
- **Private Endpoints** incur minimal additional cost but provide significant security benefits by eliminating public exposure.
- Resources are categorized as:
  - **Idle-safe**: SQL Server and Database in Basic tier, auto-pausing enabled to reduce costs.
  - **Billable**: Private Endpoint, DNS zones, and VNet resources incur ongoing charges but are necessary for secure architecture.

---

## Troubleshooting & Mental Model

- **Server vs Database IDs**:
  - The `azurerm_mssql_server` resource has its own ID; the database is a child resource linked by `server_id`.
  - Confusing the two IDs causes deployment and connectivity errors.
- **Outputs are required**:
  - Terraform outputs for server name, private endpoint IP, and DNS zone records enable integration and debugging downstream.
- **Common mistakes**:
  - Using the wrong resource ID in the private endpoint configuration.
  - Forgetting to link the private DNS zone to the VNet, causing DNS resolution failures.
  - Expecting the API to connect before secrets and identity are configured leads to premature failures.
- Understanding these helps maintain clarity and reduces debugging time.

---

## Sprint 3.4 Definition of Done

- [x] Azure SQL Server and Database provisioned with correct configuration.
- [x] Private Endpoint created in AKS subnet linked to SQL Server.
- [x] Private DNS Zone for `privatelink.database.windows.net` created and linked to VNet.
- [x] DNS A record created pointing SQL Server FQDN to private endpoint IP.
- [x] Validation of SQL connectivity from within AKS cluster pod using SQL client.
- [x] Terraform outputs documented for server name, private endpoint IP, and DNS zone.
- [x] Documentation updated with architecture, commands, and troubleshooting notes.

---

## How to Redo This in Another Project

1. **Adapt naming conventions** to your project standards.
2. **Reuse Terraform modules** for Azure SQL and Private Endpoint resources, parameterizing resource group, location, and subnet.
3. **Configure Private DNS Zones** to match the private link domain of your Azure service.
4. **Link DNS zones to your VNets** to ensure private name resolution.
5. **Validate connectivity** from your compute resources (e.g., AKS, VM) using appropriate client tools.
6. **Defer application integration** until secret management and identity are in place, following a phased rollout.
7. **Monitor costs** by selecting appropriate SKU tiers and enabling auto-pause where possible.

This approach ensures a secure, cost-effective, and maintainable Azure SQL integration with private connectivity, ready for future phases of development.
