$ResourceGroupName = "docalabs.hybridportbridge.cicd"

Function StopPostgreSQL() {

    $Name = "test-postgresql"

    Remove-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name
}

Function StopSQLServer() {
    
    $Name = "test-sql"

    Remove-AzureRmContainerGroup -ResourceGroupName $ResourceGroupName -Name $Name
}

StopPostgreSQL

StopSQLServer