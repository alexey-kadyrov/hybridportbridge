Param
(
    [Parameter(Mandatory=$True, HelpMessage="The root folder with source code")]
    [string] $SourceFolder,

    [Parameter(Mandatory=$True, HelpMessage="The root folder where the apps were published")]
    [string] $PublishedFolder
)

Function CopySettings([string]$Src, [string]$Dst) {

    $content = [System.IO.File]::ReadAllText($Src)

    $value = (Get-ChildItem Env:PortBridge:ServiceNamespace:ServiceNamespace).Value
    $content = $content.Replace("<<ServiceNamespace>>", $value)

    $value = (Get-ChildItem Env:PortBridge:ServiceNamespace:AccessRuleName).Value
    $content = $content.Replace("<<AccessRuleName>>", $value)

    $value = (Get-ChildItem Env:PortBridge:ServiceNamespace:AccessRuleKey).Value
    $content = $content.Replace("<<AccessRuleKey>>", $value)
 
    [System.IO.File]::WriteAllText($Dst, $content)
}

CopySettings -Src "$SourceFolder\Build\service-agent-config\appsettings.json" -Dst "$PublishedFolder\published-apps\DocaLabs.HybridPortBridge.ServiceAgent.WindowsService\appsettings.json"

CopySettings -Src "$SourceFolder\Build\client-agent-config\appsettings.json" -Dst "$PublishedFolder\published-apps\DocaLabs.HybridPortBridge.ClientAgent.WindowsService\appsettings.json"


$command = "$PublishedFolder\published-apps\DocaLabs.HybridPortBridge.ServiceAgent.WindowsService\DocaLabs.HybridPortBridge.ServiceAgent.WindowsService.exe install"
Invoke-Expression $command

$command = "$PublishedFolder\published-apps\DocaLabs.HybridPortBridge.ServiceAgent.WindowsService\DocaLabs.HybridPortBridge.ServiceAgent.WindowsService.exe start"
Invoke-Expression $command


$command = "$PublishedFolder\published-apps\DocaLabs.HybridPortBridge.ClientAgent.WindowsService\DocaLabs.HybridPortBridge.ClientAgent.WindowsService.exe install"
Invoke-Expression $command

$command = "$PublishedFolder\published-apps\DocaLabs.HybridPortBridge.ClientAgent.WindowsService\DocaLabs.HybridPortBridge.ClientAgent.WindowsService.exe start"
Invoke-Expression $command
