Param
(
    [Parameter(Mandatory=$False)] [string] $ResourceGroupName = "docalabs.hybridportbridge.cicd",
    [Parameter(Mandatory=$False)] [string] $Name = "test-postrgesql"
)

Remove-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name
