Param
(
    [Parameter(Mandatory=$False, HelpMessage="The resource group name where the resources should be allocated for tests")]
    [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd"
)

$Name = "RootManageSharedAccessKey"
$Namespace = "docalabs-hybridportbridge-cicd"

$Info = Get-AzureRmRelayKey -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name $Name
$PrimaryKey = $Info.PrimaryKey

$variableName = "PortBridge:ServiceNamespace:ServiceNamespace"
Write-Host "##vso[task.setvariable variable=$variableName;]docalabs-hybridportbridge-cicd.servicebus.windows.net"

$variableName = "PortBridge:ServiceNamespace:AccessRuleName"
Write-Host "##vso[task.setvariable variable=$variableName;]RootManageSharedAccessKey"

$variableName = "PortBridge:ServiceNamespace:AccessRuleKey"
Write-Host "##vso[task.setvariable variable=$variableName;]$PrimaryKey"
