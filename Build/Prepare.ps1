$ResourceGroupName = "docalabs.hybridportbridge.cicd",

Function CreatePostgreSQL() {

    $Name = "test-postgresql",
    $Image = "postgres:11.1-alpine"
    $DnsName = "docalabs-portbridge-postgresql"

    New-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name -Image $Image `
        -DnsNameLabel $DnsName `
        -OsType Linux `
        -IpAddressType Public `
        -EnvironmentVariable @{"POSTGRES_PASSWORD"="password"} `
        -Port @(5432)
}

Function CreateSQLServer() {
    
    $Name = "test-sql",
    $Image = "mcr.microsoft.com/mssql/server:2017-latest"
    $DnsName = "docalabs-portbridge-sqlserver"

    New-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name -Image $Image `
        -DnsNameLabel $DnsName `
        -OsType Linux `
        -IpAddressType Public `
        -EnvironmentVariable @{"SA_PASSWORD"="MyEdition2017!";"ACCEPT_EULA"="Y"} `
        -Port @(1433) 
}

Function PullRelayAccessKey {

    $Name = "RootManageSharedAccessKey"
    $Namespace = "docalabs-hybridportbridge-cicd"

    $Info = Get-AzureRmRelayKey -ResourceGroupName $ResourceGroupName -Namespace $Namespace -Name $Name

    $variableName = "PortBridge:ServiceNamespace:ServiceNamespace"
    Write-Host "##vso[task.setvariable variable=$variableName;]docalabs-hybridportbridge-cicd.servicebus.windows.net"    

    $variableName = "PortBridge:ServiceNamespace:AccessRuleName"
    Write-Host "##vso[task.setvariable variable=$variableName;]RootManageSharedAccessKey"    

    $variableName = "PortBridge:ServiceNamespace:AccessRuleKey"
    Write-Host "##vso[task.setvariable variable=$variableName;]$Info.PrimaryKey"    
}

CreatePostgreSQL

CreateSQLServer

PullRelayAccessKey