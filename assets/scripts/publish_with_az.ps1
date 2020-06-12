param(
 [Parameter(Mandatory=$True)] [string] $resourceGroup, 
 [Parameter(Mandatory=$True)] [string] $functionAppName
)

dotnet publish -c Release
$zipFile = 'bin\Release\netcoreapp3.1\publish.zip'
Compress-Archive -Path bin\Release\netcoreapp3.1\publish\* -DestinationPath $zipFile -Force
az functionapp deployment source config-zip -g $resourceGroup -n $functionAppName --src $zipFile

# ..\publish_with_az.ps1 -resourceGroup rg-0906-1 -functionAppName func-0906-1