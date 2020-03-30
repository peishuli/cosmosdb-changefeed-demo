using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Shared;

namespace ChangeFeedConsole
{
    class Program
    {
        private static readonly string _endpointUrl = "https://localhost:8081";
        private static readonly string _primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private static readonly string _databaseId = "StoreDatabase";
        private static readonly string _containerId = "CartContainer";

        private static readonly string _destinationContainerId = "CartContainerByState";

        private static CosmosClient cosmosClient = new CosmosClient(_endpointUrl, _primaryKey);

        static async Task Main(string[] args)
        {
                var db = cosmosClient.GetDatabase(_databaseId);
                var container = db.GetContainer(_containerId);
                var destinationContainer = db.GetContainer(_destinationContainerId);

                ContainerProperties leaseContainerProperties = new ContainerProperties("consoleLeases", "/id");
                Container leaseContainer = await db.CreateContainerIfNotExistsAsync(leaseContainerProperties, throughput: 400);
                var builder = container.GetChangeFeedProcessorBuilder("migrationProcessor", 
                    (IReadOnlyCollection<CartAction> input, CancellationToken cancellationToken) => {
                    Console.WriteLine(input.Count + " Changes Received");
                        var tasks = new List<Task>();
                        foreach (var doc in input)
                        {
                            tasks.Add(destinationContainer.CreateItemAsync(doc, new PartitionKey(doc.BuyerState)));
                        }
                        return Task.WhenAll(tasks);
                    });

                var processor = builder
                    .WithInstanceName("changeFeedConsole")
                    .WithLeaseContainer(leaseContainer)
                    .Build();

                await processor.StartAsync();
            
                Console.WriteLine("Started Change Feed Processor");
                Console.WriteLine("Press any key to stop the processor...");

                Console.ReadKey();

                Console.WriteLine("Stopping Change Feed Processor");

                //todo: Add stop code here
         }
      }
}