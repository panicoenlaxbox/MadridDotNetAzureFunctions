param(
 [Parameter(Mandatory=$True)] [string] $resourceGroup,
 [Parameter(Mandatory=$True)] [string] $location,
 [Parameter(Mandatory=$True)] [string] $storageAccount,
 [Parameter(Mandatory=$True)] [string] $appInsights,
 [Parameter(Mandatory=$True)] [string] $functionAppName
)

az group create -n $resourceGroup -l $location

az storage account create -n $storageAccount -l $location -g $resourceGroup --sku Standard_LRS

az resource create -g $resourceGroup -n $appInsights `
--resource-type "Microsoft.Insights/components" `
--properties '{ \"Application_Type\": \"web\" }'

az functionapp create -n $functionAppName -g $resourceGroup `
--storage-account $storageAccount `
--app-insights $appInsights `
--consumption-plan-location $location `
--runtime dotnet `
--functions-version 3

# .\create_resources_with_az.ps1 -resourceGroup rg-0906-1 -location westeurope -storageAccount storageaccount09061 -appInsights ai-0906-1 -functionAppName func-0906-1