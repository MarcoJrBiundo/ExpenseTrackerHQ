locals {
  prefix = "expensetracker-dev"

  tags = {
    project = "ExpenseTrackerHQ"
    env     = "dev"
  }
}

module "networking" {
  source = "../../modules/networking"

  rg_name  = "rg-expensetracker-network-dev"
  location = "canadacentral"

  vnet_name = "vnet-expensetracker-dev"
  vnet_cidr = "10.0.0.0/16"

  aks_subnet_name = "snet-aks"
  aks_subnet_cidr = "10.0.1.0/24"

  sql_subnet_name = "snet-sql"
  sql_subnet_cidr = "10.0.2.0/24"
  aks_nsg_name    = "nsg-expensetracker-aks-dev"
  sql_nsg_name    = "nsg-expensetracker-sql-dev"
}

module "acr" {
  source   = "../../modules/acr"
  rg_name  = module.networking.rg_name
  location = module.networking.location
  acr_name = "acrexptrackerhqdev01"
  sku      = "Basic"
  tags     = local.tags
}

module "aks" {
  source              = "../../modules/aks"
  rg_name             = module.networking.rg_name
  location            = module.networking.location
  aks_name            = "aks-expensetracker-dev"
  dns_prefix          = "aks-expense-dev"
  subnet_id           = module.networking.aks_subnet_id
  kubernetes_version  = null
  system_node_count   = 1
  system_node_vm_size = "Standard_B2s_v2"
  service_cidr        = "10.1.0.0/16"
  dns_service_ip      = "10.1.0.10"
  user_node_vm_size   = "Standard_B2s_v2"
  user_node_min       = 0
  user_node_max       = 2
  tags                = local.tags
}

module "sql" {
  source          = "../../modules/sql"
  sql_server_name = "sql-expensetracker-dev-mb1319"
  rg_name         = module.networking.rg_name
  location        = module.networking.location
  admin_login     = var.sql_admin_login
  admin_password  = var.sql_admin_password
  sql_db_name     = "sqldb-expensetracker-dev"
  tags            = local.tags
}

module "private_dns_sql" {
  source   = "../../modules/private-dns-sql"
  rg_name  = module.networking.rg_name
  location = module.networking.location
  vnet_id  = module.networking.vnet_id
  tags     = local.tags
}


module "private_endpoint_sql" {
  source                = "../../modules/private-endpoint-sql"
  rg_name               = module.networking.rg_name
  location              = module.networking.location
  subnet_id             = module.networking.sql_subnet_id
  private_endpoint_name = "pe-sql-expensetracker-dev"
  sql_server_id         = module.sql.sql_server_id
  private_dns_zone_id   = module.private_dns_sql.private_dns_zone_id
  tags                  = local.tags
}

module "keyvault" {
  source              = "../../modules/keyvault"
  name                = "${local.prefix}-kv"
  location            = module.networking.location
  resource_group_name = module.networking.rg_name
  tags                = local.tags
}


