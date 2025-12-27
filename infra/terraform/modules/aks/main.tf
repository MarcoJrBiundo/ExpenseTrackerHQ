resource "azurerm_kubernetes_cluster" "aks" {
  name                      = var.aks_name
  location                  = var.location
  resource_group_name       = var.rg_name
  dns_prefix                = var.dns_prefix
  kubernetes_version        = var.kubernetes_version
  oidc_issuer_enabled       = true
  workload_identity_enabled = true
  identity {
    type = "SystemAssigned"
  }
  default_node_pool {
    name                        = "system"
    vm_size                     = var.system_node_vm_size
    vnet_subnet_id              = var.subnet_id
    type                        = "VirtualMachineScaleSets"
    enable_auto_scaling         = false
    node_count                  = var.system_node_count
    orchestrator_version        = var.kubernetes_version
    temporary_name_for_rotation = "systemtmp"

  }
  network_profile {
    network_plugin = "azure"
    service_cidr   = var.service_cidr
    dns_service_ip = var.dns_service_ip
  }
  tags = var.tags
}

resource "azurerm_kubernetes_cluster_node_pool" "user" {
  name                  = "user"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.aks.id

  mode           = "User"
  vm_size        = var.user_node_vm_size
  vnet_subnet_id = var.subnet_id

  orchestrator_version = var.kubernetes_version

  enable_auto_scaling = true
  min_count           = var.user_node_min
  max_count           = var.user_node_max

  tags = var.tags
}
