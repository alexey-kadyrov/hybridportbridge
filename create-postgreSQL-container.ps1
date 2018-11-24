Param
(
    [Parameter(Mandatory=$False)] [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd",
    [Parameter(Mandatory=$False)] [string] $Name = "test-postrgesql",
    [Parameter(Mandatory=$False)] [string] $Image = "postgres:11.1-alpine"
)

$DnsName = "$ResourceGroupName-$Name".Replace(".", "-")

New-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name -Image $Image `
 -DnsNameLabel $DnsName `
 -OsType Linux `
 -IpAddressType Public `
 -EnvironmentVariable @{"POSTGRES_PASSWORD"="password"} `
 -Port @(5432)
