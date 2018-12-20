Param
(
    [Parameter(Mandatory=$False, HelpMessage="The resource group name where the resources should be allocated for tests")]
    [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd"
)

$Namespace = (Get-ChildItem Env:PortBridge:ServiceNamespace:ServiceNamespace).Value

Remove-AzureRmRelayNamespace -ResourceGroupName $ResourceGroupName -Name $Namespace | Out-Null
