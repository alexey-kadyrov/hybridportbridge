Param
(
    [Parameter(Mandatory=$True)] [string] $versionFileName,
    [Parameter(Mandatory=$True)] [string] $branchName,
    [Parameter(Mandatory=$True)] [string] $buildId
)

Function Update-Versions {

    $xml=New-Object XML

    $xml.Load($versionFileName)

    $ns = New-Object Xml.XmlNamespaceManager($xml.NameTable)

    $versionPrexifNode = $xml.SelectSingleNode('/Project/PropertyGroup/VersionPrefix')
    $versionPrexifSplit = $versionPrexifNode.InnerText.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)

    $versionPrexif = $versionPrexifSplit[0] + "." + $versionPrexifSplit[1] + "." + $patchNumber

    $versionPrexifNode.InnerText = $versionPrexif

    $versionSuffixNode = $xml.SelectSingleNode('/Project/PropertyGroup/VersionSuffix')

    if($versionSuffix -ne "") {
        $packageVersion = "$versionPrexif-$versionSuffix"
        $versionSuffixNode.InnerText = $versionSuffix
    }
    else {
        $packageVersion = "$versionPrexif"
        $xml.SelectSingleNode('/Project/PropertyGroup').RemoveChild($versionSuffixNode) | Out-Null
    }

    $xml.Save($versionFileName)

    $variableName = "myPackageVersion"

    Write-Host "Version generated for $variableName is $packageVersion"
    Write-Host "##vso[task.setvariable variable=$variableName;]$packageVersion"
}

Function IsMaster {

    if($branchName -eq "master") {
        return $True
    }
    else {
        return $False
    }
}

# Execution starts here

$branchName = $branchName.ToLowerInvariant()

$versionSuffix = ""

if(IsMaster -eq $True) {
    $versionSuffix = ""
}
else {
    $versionSuffix = "alpha"
}

$patchNumber = ""
foreach($ch in $buildId.ToCharArray()) {
    if(($ch -ge '0') -and ($ch -le '9')) {
        $patchNumber += $ch
    }
}

if($patchNumber.Length -eq 0) {
    $patchNumber = "0"
}

Update-Versions

$content = [System.IO.File]::ReadAllText($versionFileName)
Write-Host "-------------- Will be using versions..."
Write-Host $content
Write-Host "----------------------------------------"