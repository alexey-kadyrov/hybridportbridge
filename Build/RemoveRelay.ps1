Param
(
    [Parameter(Mandatory=$False, HelpMessage="The resource group name where the resources should be allocated for tests")]
    [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd"
)

$Namespace = (Get-ChildItem Env:PortBridge:ServiceNamespace:ServiceNamespace).Value
If($Namespace.EndsWith(".servicebus.windows.net") -eq $True) {
    $Namespace = $Namespace.Substring(0, $Namespace.Length - ".servicebus.windows.net".Length)
}

Remove-AzureRmRelayNamespace -ResourceGroupName $ResourceGroupName -Name $Namespace | Out-Null
