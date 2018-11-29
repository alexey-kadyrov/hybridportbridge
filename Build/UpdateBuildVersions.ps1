<#
    .DESCRIPTION
        Modifies the version and helm chart files which is used to generate nuget and helm packages during the build
#>
Param
(
    [Parameter(Mandatory=$True, HelpMessage="The source control branch name, it's used to set the version suffix: for master, release, hotfix it's empty, for develop it's beta for anything else it's alpha")] 
    [string] $BranchName,

    [Parameter(Mandatory=$False, HelpMessage="The base folder where the version.xml file and _charts sub folder are located. If relative path is specified it'll be expanded using the location of the script file")] 
    [string] $BaseFolder,

    [Parameter(Mandatory=$False, HelpMessage="The unique build id like 25054 assigned by VSTS, either buildId or BuildNumber must be specified, if none is specified then the buildId will be always 1. If BuildId is specified it'll use the major and minor version from the VersionPrefix in the file and replace only the patch number")] 
    [string] $BuildId,

    [Parameter(Mandatory=$False, HelpMessage="The Build Number, usually (build.buildNumber) assigned by VSTS, either buildId or BuildNumber must be specified, if none is specified then the buildId will be always 1. If BuildNumber is specified it'll completely replace the VersionPrefix in the file")] 
    [string] $BuildNumber
)

Function Get-VersionPrefix()
{
    if([String]::IsNullOrWhiteSpace($BuildNumber)) {
        if([String]::IsNullOrWhiteSpace($BuildId)) {
            $BuildId = "1"
        }
        $versionPrefixSplit = $versionPrefixNode.InnerText.Split('.',[System.StringSplitOptions]::RemoveEmptyEntries)

        $patchNumber = ""
        foreach($ch in $BuildId.ToCharArray()) {
            if(($ch -ge '0') -and ($ch -le '9')) {
                $patchNumber += $ch
            }
        }
        if($patchNumber.Length -eq 0) {
            $patchNumber = "0"
        }

        $versionPrefix = $versionPrefixSplit[0] + "." + $versionPrefixSplit[1] + "." + $patchNumber
    }
    else {
        $versionPrefix = $BuildNumber
    }

    return $versionPrefix
}

Function Update-Versions([string] $file) {

    $xml = New-Object XML
    
    $xml.Load($file)

    New-Object Xml.XmlNamespaceManager($xml.NameTable) | Out-Null

    $versionPrefixNode = $xml.SelectSingleNode('/Project/PropertyGroup/VersionPrefix')

    $versionPrefix = Get-VersionPrefix($versionPrefixNode)

    $versionPrefixNode.InnerText = $versionPrefix

    $versionSuffixNode = $xml.SelectSingleNode('/Project/PropertyGroup/VersionSuffix')

    if($versionSuffix -ne "") {
        $packageVersion = "$versionPrefix-$versionSuffix"
        $versionSuffixNode.InnerText = $versionSuffix
    }
    else {
        $packageVersion = "$versionPrefix"
        $xml.SelectSingleNode('/Project/PropertyGroup').RemoveChild($versionSuffixNode) | Out-Null
    }

    $xml.Save($file) | Out-Null

    $variableName = "myPackageVersion"

    Write-Host "Version generated for $variableName is $packageVersion"
    Write-Host "##vso[task.setvariable variable=$variableName;]$packageVersion"

    return $packageVersion
}

Function Update-Chart-Version([Parameter(Position=0)] [string] $File, [Parameter(Position=1)] [string] $Version) {

    $Lines = [System.IO.File]::ReadAllLines($File)

    For($i = 0; $i -lt $Lines.Length; $i++) {
        if($Lines[$i].StartsWith("version:")) {
            $Lines[$i] = "version: $Version"
        }
    }

    [System.IO.File]::WriteAllLines($File, $Lines) | Out-Null
}

Function IsMaster {

    if($BranchName -eq "master") {
        return $True
    }
    else {
        return $False
    }
}

#******************************************************************************
# Script body
# Execution begins here
#******************************************************************************

$BranchName = $BranchName.ToLowerInvariant()

Write-Host
Write-Host "Processing parameters:"
Write-Host "BranchName=$BranchName"
Write-Host "BaseFolder=$BaseFolder"
Write-Host "BuildId=$BuildId"
Write-Host "BuildNumber=$BuildNumber"
Write-Host

$versionSuffix = ""

if(IsMaster -eq $True) {
    $versionSuffix = ""
}
else
{
    $versionSuffix = "alpha"
}

If($BaseFolder.StartsWith(".")) {
    $BaseFolder = [System.IO.Path]::Combine($PSScriptRoot, $BaseFolder)
    $BaseFolder = [System.IO.Path]::GetFullPath($BaseFolder)
}

$file = [System.IO.Path]::Combine($BaseFolder, "version.xml")

Write-Host
Write-Host "Updating Assembly Versions"
$version = Update-Versions $file


$content = [System.IO.File]::ReadAllText($file)

Write-Host
Write-Host "-------------- Will be using assembly versions..."
Write-Host $content
Write-Host "----------------------------------------"
Write-Host


Write-Host
Write-Host "Updating Chart Versions"

$chartsFolder = [System.IO.Path]::Combine($BaseFolder, "_charts")

Write-Host "Cheking Charts folder: $chartsFolder"

Get-ChildItem -Path $chartsFolder -Filter Chart.yaml -Recurse -File | ForEach-Object {

    Update-Chart-Version $_.FullName $version
}