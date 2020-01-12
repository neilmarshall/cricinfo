namespace DbCreate

module ComputationRoot =

    let scriptExecutor =
        ScriptExecutor.executeScriptsAsync
            PostgresDataManager.getColumn
            PostgresDataManager.executeScript
            PostgresDataManager.insertData