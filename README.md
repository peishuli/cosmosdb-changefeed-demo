# Cosmos DB Change Feed Demo
This demo uses Cosmos DB Emulator, it is adapted from this [workshop](https://cosmosdb.github.io/labs/dotnet/labs/08-change_feed_with_azure_functions.html).

## Prerequisites
Follow the following steps to set up the Cosmos DB database and containers:
1. Create a Cosmos DB database _StoreDatabase_
2. Under the StoreDatabase, create three containers
   * A _CartContainer_ container with _Item_ as the Partition Key and allocate 11,000 RUs
   * A _CartContainerByState_ container with _BuyerState_ as the Partition Key and allocate 11,000 RUs
   * A _StateSales_ container with _State_ as the Partition Key and allocate 11,000 RUs
 
 ## Run the demo
 Follow the following steps to run the demo:
 1. Open ChangeFeedConsole.sln in Visual Studio 2019 and set both _ChangeFeedConsole_ and _CosmosDBTrigger_ as Startup projects press F5 to run.
 2. Open a command line window and navigate to the DataGenerator folder, execute _dotnet run_ command.

 ## How does it work?
 1. DataGenerator uses the Bogus NuGet package to generate random data and insert documents into the CartContainer container. 
 2. ChangeFeedConsole uses the Change Feed Processor Library to pull change feeds from the CartContainer container with Item as the Partition Key and insert documents of the same schema into the CartContainerByState container with a different Partition key, BuyerState.
 3. CosmosDBTrigger is an Azure function that is triggered by Cosmos DB change feed. It transforms the document into a different schema and either insert or update a materialized view that is the StateSales container.   
