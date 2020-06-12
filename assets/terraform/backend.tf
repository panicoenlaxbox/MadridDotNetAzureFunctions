terraform {
  required_version = "= 0.12.26"
  backend "azurerm" {
    resource_group_name  = "rg-terraform"
    storage_account_name = "backend0906"
    container_name       = "tfstate"
    key                  = "my_project.terraform.tfstate"
  }
}