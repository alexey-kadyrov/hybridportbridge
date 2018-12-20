Param
(
    [Parameter(Mandatory=$False, HelpMessage="The resource group name where the resources should be allocated for tests")]
    [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd"
)

$Namespace = (Get-ChildItem Env:PortBridge:ServiceNamespace:ServiceNamespace).Value
$Name = (Get-ChildItem Env:PortBridge:ServiceNamespace:AccessRuleName).Value

$Info = Get-AzureRmRelayKey -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name $Name
$PrimaryKey = $Info.PrimaryKey

$variableName = "PortBridge:ServiceNamespace:AccessRuleKey"
Write-Host "##vso[task.setvariable variable=$variableName;]$PrimaryKey"

$variableName = "PortBridge__ServiceNamespace__AccessRuleKey"
Write-Host "##vso[task.setvariable variable=$variableName;]$PrimaryKey"
