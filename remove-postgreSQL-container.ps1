Param
(
    [Parameter(Mandatory=$False)] [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd",
    [Parameter(Mandatory=$False)] [string] $Name = "test-postgresql"
)

Remove-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name
