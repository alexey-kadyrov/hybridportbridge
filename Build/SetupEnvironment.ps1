Param
(
    [Parameter(Mandatory=$False, HelpMessage="The style of the environment variables for the relay access")]
    [bool] $LinuxStyleEnvVariables = $False,
    
    [Parameter(Mandatory=$False, HelpMessage="The resource group name where the resources should be allocated for tests")]
    [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd"
)

Function SetRelayAccessKeyEnvVariables {

    $Name = "RootManageSharedAccessKey"
    $Namespace = "docalabs-hybridportbridge-cicd"

    $Info = Get-AzureRmRelayKey -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name $Name
    $PrimaryKey = $Info.PrimaryKey

    If($LinuxStyleEnvVariables -eq $True)     {
        $variableName = "PortBridge__ServiceNamespace__ServiceNamespace"
    } else {
        $variableName = "PortBridge:ServiceNamespace:ServiceNamespace"
    }
    Write-Host "##vso[task.setvariable variable=$variableName;]docalabs-hybridportbridge-cicd.servicebus.windows.net"

    If($LinuxStyleEnvVariables -eq $True)     {
        $variableName = "PortBridge__ServiceNamespace__AccessRuleName"
    } else {
        $variableName = "PortBridge:ServiceNamespace:AccessRuleName"
    }
    Write-Host "##vso[task.setvariable variable=$variableName;]RootManageSharedAccessKey"

    If($LinuxStyleEnvVariables -eq $True) {
        $variableName = "PortBridge__ServiceNamespace__AccessRuleKey"
    } else {
        $variableName = "PortBridge:ServiceNamespace:AccessRuleKey"
    }
    Write-Host "##vso[task.setvariable variable=$variableName;]$PrimaryKey"    
}

SetRelayAccessKeyEnvVariables