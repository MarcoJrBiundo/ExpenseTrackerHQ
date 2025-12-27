output "aks_id" {
  value = azurerm_kubernetes_cluster.aks.id
}

output "aks_name" {
  value = azurerm_kubernetes_cluster.aks.name
}

output "aks_rg_name" {
  value = azurerm_kubernetes_cluster.aks.resource_group_name
}
output "aks_resource_group" {
  value = azurerm_kubernetes_cluster.aks.node_resource_group
}

output "aks_tenant_id" {
  value = azurerm_kubernetes_cluster.aks.identity[0].tenant_id
}

output "aks_principal_id" {
  value = azurerm_kubernetes_cluster.aks.identity[0].principal_id
}

output "aks_identity_type" {
  value = azurerm_kubernetes_cluster.aks.identity[0].type
}


output "kubelet_object_id" {
  value = try(azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id, null)
}
