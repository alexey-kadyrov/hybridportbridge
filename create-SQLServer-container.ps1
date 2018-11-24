Param
(
    [Parameter(Mandatory=$False)] [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd",
    [Parameter(Mandatory=$False)] [string] $Name = "test-sql",
    [Parameter(Mandatory=$False)] [string] $Image = "microsoft/mssql-server-linux:2017-CU12"
)

$DnsName = "$ResourceGroupName-$Name".Replace(".", "-")

New-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name -Image $Image `
 -DnsNameLabel $DnsName `
 -OsType Linux `
 -IpAddressType Public `
 -EnvironmentVariable @{"SA_PASSWORD"="MyEdition2017!";"ACCEPT_EULA"="Y"} `
 -Port @(14333; 1433)
