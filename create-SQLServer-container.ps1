Param
(
    [Parameter(Mandatory=$False)] [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd",
    [Parameter(Mandatory=$False)] [string] $Name = "test-sql",
    [Parameter(Mandatory=$False)] [string] $Image = "mcr.microsoft.com/mssql/server:2017-latest"
)

$DnsName = "docalabs-portbridge-sqlserver"

New-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name -Image $Image `
 -DnsNameLabel $DnsName `
 -OsType Linux `
 -IpAddressType Public `
 -EnvironmentVariable @{"SA_PASSWORD"="MyEdition2017!";"ACCEPT_EULA"="Y"} `
 -Port @(1433)
