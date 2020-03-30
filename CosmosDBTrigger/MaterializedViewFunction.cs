using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared;

namespace CosmosDBTrigger
{
    public static class MaterializedViewFunction
    {
        private static readonly string _endpointUrl = "https://localhost:8081";
        private static readonly string _primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private static readonly string _databaseId = "StoreDatabase";
        private static readonly string _containerId = "StateSales";
        private static CosmosClient cosmosClient = new CosmosClient(_endpointUrl, _primaryKey);

        [FunctionName("MaterializedViewFunctions")]
        public static async Task RunAsync([CosmosDBTrigger(
            databaseName: "StoreDatabase",
            collectionName: "CartContainerByState",
            ConnectionStringSetting = "CosmosDBConnectionString",
            CreateLeaseCollectionIfNotExists = true,
            LeaseCollectionName = "materializedViewLeases")]IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                var stateDict = new Dictionary<string, List<double>>();
                foreach (var doc in input)
                {
                    var action = JsonConvert.DeserializeObject<CartAction>(doc.ToString());

                    if (action.Action != ActionType.Purchased)
                    {
                        continue;
                    }

                    if (stateDict.ContainsKey(action.BuyerState))
                    {
                        stateDict[action.BuyerState].Add(action.Price);
                    }
                    else
                    {
                        stateDict.Add(action.BuyerState, new List<double> { action.Price });
                    }
                }
                var db = cosmosClient.GetDatabase(_databaseId);
                var container = db.GetContainer(_containerId);

                var tasks = new List<Task>();

                foreach (var key in stateDict.Keys)
                {
                    var query = new QueryDefinition("select * from StateSales s where s.State = @state").WithParameter("@state", key);

                    var resultSet = container.GetItemQueryIterator<StateCount>(query, requestOptions: new QueryRequestOptions() { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(key), MaxItemCount = 1 });

                    while (resultSet.HasMoreResults)
                    {
                        var stateCount = (await resultSet.ReadNextAsync()).FirstOrDefault();

                        if (stateCount == null)
                        {
                            stateCount = new StateCount
                            {
                                State = key,
                                TotalSales = stateDict[key].Sum(),
                                Count = stateDict[key].Count
                            };
                        }
                        else
                        {
                            stateCount.TotalSales += stateDict[key].Sum();
                            stateCount.Count += stateDict[key].Count;
                        }

                        log.LogInformation("Upserting materialized view document");
                        tasks.Add(container.UpsertItemAsync(stateCount, new Microsoft.Azure.Cosmos.PartitionKey(stateCount.State)));
                    }
                }

                await Task.WhenAll(tasks);
            }
        }
    }
}
