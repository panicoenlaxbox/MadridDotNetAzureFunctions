# terraform init
# terraform plan -out=tfplan
# terraform apply -auto-approve tfplan
# terraform output my_function_app_name

provider "azurerm" {
  # az login
  subscription_id = "TODO"
  tenant_id       = "TODO"
  version         = "~> 2.8"
  features {}
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "storage_account_name" {
  type = string
}

variable "application_insights_name" {
  type = string
}

variable "app_service_plan_name" {
  type = string
}

variable "function_app_name" {
  type = string
}

resource "azurerm_resource_group" "my_resource_group" {
  name     = var.resource_group_name
  location = var.location
}

resource "azurerm_storage_account" "my_storage_account" {
  name                     = var.storage_account_name
  resource_group_name      = azurerm_resource_group.my_resource_group.name
  location                 = azurerm_resource_group.my_resource_group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_application_insights" "my_application_insights" {
  name                = var.application_insights_name
  resource_group_name = azurerm_resource_group.my_resource_group.name
  location            = azurerm_resource_group.my_resource_group.location
  application_type    = "other"
}

resource "azurerm_app_service_plan" "my_app_service_plan" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.my_resource_group.name
  location            = azurerm_resource_group.my_resource_group.location
  kind                = "FunctionApp"

  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

resource "azurerm_function_app" "my_function_app" {
  name                       = var.function_app_name
  resource_group_name        = azurerm_resource_group.my_resource_group.name
  location                   = azurerm_resource_group.my_resource_group.location
  version                    = "~3"
  app_service_plan_id        = azurerm_app_service_plan.my_app_service_plan.id
  storage_account_name       = azurerm_storage_account.my_storage_account.name
  storage_account_access_key = azurerm_storage_account.my_storage_account.primary_access_key
  enable_builtin_logging     = false

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.my_application_insights.instrumentation_key
    "FUNCTIONS_WORKER_RUNTIME"       = "dotnet"
    "WEBSITE_RUN_FROM_PACKAGE"       = "1"
  }
}

output "my_function_app_name" {
  value = azurerm_function_app.my_function_app.name
}