using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.IO;

namespace HGV.Aquila.Functions
{
    public class EliminationFunction
    {
        public EliminationFunction()
        {
        }

        [FunctionName("EliminationCreateTournament")]
        public async Task<IActionResult> CreateTournament(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "elimination-tournaments",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "elimination")] HttpRequest req
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            var id = Guid.NewGuid();
            token.Add("id", id);
            await collector.AddAsync(token);
            return new CreatedResult($"/elimination/tournament/{id}", new { id });
        }

        [FunctionName("EliminationGetTournament")]
        public IActionResult GetTournament(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "elimination-tournaments",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")]JObject item,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "elimination/{id}")] HttpRequest req, Guid id
        )
        {
            if(item == null)
                return new NotFoundResult();
            else
                return new OkObjectResult(item);
        }

        [FunctionName("EliminationPutTournament")]
        public async Task<IActionResult> PutTournament(
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "elimination-tournaments",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "elimination/{id}")] HttpRequest req, Guid id
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            await collector.AddAsync(token);

            return new OkResult();
        }

    }
}
