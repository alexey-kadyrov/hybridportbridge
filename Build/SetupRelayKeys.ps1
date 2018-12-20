Param
(
    [Parameter(Mandatory=$False, HelpMessage="The resource group name where the resources should be allocated for tests")]
    [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd",

    [Parameter(Mandatory=$False, HelpMessage="The relay location")]
    [string] $Location = "northeurope"
)


$Namespace = (Get-ChildItem Env:PortBridge:ServiceNamespace:ServiceNamespace).Value
If($Namespace.EndsWith(".servicebus.windows.net") -eq $True) {
    $Namespace = $Namespace.Substring(0, $Namespace.Length - ".servicebus.windows.net".Length)
}

$RuleName = (Get-ChildItem Env:PortBridge:ServiceNamespace:AccessRuleName).Value

$Info = Get-AzureRmRelayKey -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name $RuleName
$PrimaryKey = $Info.PrimaryKey

# windows is in the same agent job so the ENV variable is fine
$variableName = "PortBridge:ServiceNamespace:AccessRuleKey"
Write-Host "##vso[task.setvariable variable=$variableName;]$PrimaryKey"

# linux will be in the different agent - so update the gorup variables
$headers = @{ Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN" }
$url= "https://dev.azure.com/alexeikadyrov/hybridportbridge/_apis/distributedtask/variablegroups/1?api-version=5.0-preview.1"
$body = "{
  `"variables`": {
    `"PortBridge__ServiceNamespace__AccessRuleKey`": {
      `"value`": `"$PrimaryKey`",
      `"isSecret`": true
    }
  },
  `"type`": `"Vsts`",
  `"name`": `"docalabs.hybridportbridge.cicd-relay`",
  `"description`": `"Updated variable group`"
}"
 
Invoke-RestMethod -Uri $url  -ContentType "application/json" -Body $body -headers $headers -Method PUT | Out-Null
