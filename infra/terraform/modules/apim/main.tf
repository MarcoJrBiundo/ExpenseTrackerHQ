resource "azurerm_api_management" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  publisher_email     = var.publisher_email
  publisher_name      = var.publisher_name

  public_network_access_enabled = true

  sku_name = var.sku_name
  identity {
    type = "SystemAssigned"
  }
  tags = var.tags
}
