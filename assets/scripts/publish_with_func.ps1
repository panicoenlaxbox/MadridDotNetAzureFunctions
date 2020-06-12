param(
 [Parameter(Mandatory=$True)] [string] $functionAppName
)

func azure functionapp publish $functionAppName

# ..\publish_with_func.ps1 -functionAppName func-0906-1