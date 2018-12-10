Param
(
    [Parameter(Mandatory=$True, HelpMessage="Where to read the relay primary key")]
    [string] $DropFolder
)

$file = "$DropFolder/relay-primary-key.txt"

$PrimaryKey = [System.IO.File]::ReadAllText($file)

$variableName = "PortBridge__ServiceNamespace__ServiceNamespace"
Write-Host "##vso[task.setvariable variable=$variableName;]docalabs-hybridportbridge-cicd.servicebus.windows.net"

$variableName = "PortBridge__ServiceNamespace__AccessRuleName"
Write-Host "##vso[task.setvariable variable=$variableName;]RootManageSharedAccessKey"

$variableName = "PortBridge__ServiceNamespace__AccessRuleKey"
Write-Host "##vso[task.setvariable variable=$variableName;]$PrimaryKey"
