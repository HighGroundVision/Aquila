using HGV.Basilius.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Aquila
{
    public class CaptainsDraftFunction
    {
        private readonly IMetaClient metaClient;

        public CaptainsDraftFunction(IMetaClient metaClient)
        {
            this.metaClient = metaClient;
        }

        [FunctionName("CaptainsDraftNegotiate")]
        public async Task<SignalRConnectionInfo> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "captains/{id}/negotiate")] HttpRequest req,
            string id,
            IBinder binder)
        {
            var attr = new SignalRConnectionInfoAttribute()
            {
                ConnectionStringSetting = "SignalRConnectionString",
                HubName = "CaptainsDraft",
                UserId = id,
            };
            var connectionInfo = await binder.BindAsync<SignalRConnectionInfo>(attr);
            return connectionInfo;
        }

          [FunctionName("CaptainsDraftCreate")]
        public async Task<IActionResult> CreateDraft(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "captains-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "captains")] HttpRequest req
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            var id = Guid.NewGuid();
            token.Add("id", id);
            await collector.AddAsync(token);
            return new CreatedResult($"/captains/{id}", new { id });
        }

        [FunctionName("CaptainsDraftGet")]
        public IActionResult GetDraft(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "captains-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")]JObject item,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "captains/{id}")] HttpRequest req, Guid id
        )
        {
            if(item == null)
                return new NotFoundResult();
            else
                return new OkObjectResult(item);
        }

        [FunctionName("CaptainsDraftSelection")]
        public async Task<IActionResult> Selection(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "captains-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")]JObject document,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "captains-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "captains/{id}/selection")] HttpRequest req,
            string id,
            [SignalR(HubName = "CaptainsDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            if(document == null)
                return new NotFoundResult();

            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);
            
            var phaseIndex = token["phase"].Value<int>();
            document["phase"] = phaseIndex + 1;

            var sequence = document["sequence"] as JArray;
            var item = sequence[phaseIndex];
            var phase = item["phase"].Value<int>();
            var hero = token["hero"].Value<int>();

            item["selection"] = hero;

            if(phase == 1)
            {
                var replacements = metaClient.GetHeroes().Where(_ => _.AbilityReplaceRequired == true).Select(_ => _.Id).ToList();
                if(replacements.Contains(hero))
                {
                    var pool = document["pool"].ToObject<List<int>>();
                    pool.RemoveAll(_ => replacements.Contains(_));
                    document["pool"] = new JArray(pool);
                }
            }
            
            await collector.AddAsync(document);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "update",
                Arguments = new object[] { phaseIndex }
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }
        
        [FunctionName("CaptainsDraftStart")]
        public async Task<IActionResult> Start(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "captains-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")]JObject document,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "captains-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "captains/{id}/Start")] HttpRequest req,
            string id,
            [SignalR(HubName = "CaptainsDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            if(document == null)
                return new NotFoundResult();

             document["started"] = true;

            await collector.AddAsync(document);

            var phaseIndex = 0;
            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "update",
                Arguments = new object[] { phaseIndex }
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }
    }
}