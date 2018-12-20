Param
(
    [Parameter(Mandatory=$False, HelpMessage="The resource group name where the resources should be allocated for tests")]
    [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd",

    [Parameter(Mandatory=$False, HelpMessage="The relay location")]
    [string] $Location = "northeurope"
)

$Namespace = (Get-ChildItem Env:PortBridge:ServiceNamespace:ServiceNamespace).Value
$RuleName = (Get-ChildItem Env:PortBridge:ServiceNamespace:AccessRuleName).Value

New-AzureRmRelayNamespace -ResourceGroupName $ResourceGroupName -Name $Namespace -Location $Location | Out-Null

$m = '[{"key":"5010","value":"tcp:localhost:5010"},{"key":"5011","value":"tcp:localhost:5011"},{"key":"5012","value":"tcp:non-existing-host-abc-42187:80"},{"key":"key-to-ignore","value":"localhost:80"},{"key":"5013","value":"tcp:localhost:5013"}]'
New-AzureRmRelayHybridConnection -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name "simple" -UserMetadata $m | Out-Null

$m = '[{"key":"14333","value":"tcp:docalabs-portbridge-sqlserver.northeurope.azurecontainer.io:1433"},{"key":"5432","value":"tcp:docalabs-portbridge-postgresql.northeurope.azurecontainer.io:5432"},{"key":"3306","value":"tcp:localhost:3306"}]'
New-AzureRmRelayHybridConnection -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name "sql" -UserMetadata $m | Out-Null

$m = '[{"key":"5111","value":"tcp:$(e):5011"}]'
New-AzureRmRelayHybridConnection -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name "localhost" -UserMetadata $m | Out-Null

$m = '[{"key":"5010","value":"tcp:localhost:5010"}]'
New-AzureRmRelayHybridConnection -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name "client-cert" -UserMetadata $m | Out-Null


$Info = Get-AzureRmRelayKey -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name $RuleName
$PrimaryKey = $Info.PrimaryKey

$variableName = "PortBridge:ServiceNamespace:AccessRuleKey"
Write-Host "##vso[task.setvariable variable=$variableName;]$PrimaryKey"

$variableName = "PortBridge__ServiceNamespace__AccessRuleKey"
Write-Host "##vso[task.setvariable variable=$variableName;]$PrimaryKey"
