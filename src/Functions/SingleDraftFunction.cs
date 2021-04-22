using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Aquila
{
    public class SingleDraftFunction
    {
        public SingleDraftFunction()
        {
        }

        [FunctionName("SingleDraftNegotiate")]
        public async Task<SignalRConnectionInfo> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/negotiate")] HttpRequest req,
            string id,
            IBinder binder)
        {
            var attr = new SignalRConnectionInfoAttribute()
            {
                ConnectionStringSetting = "SignalRConnectionString",
                HubName = "SingleDraft",
                UserId = id,
            };
            var connectionInfo = await binder.BindAsync<SignalRConnectionInfo>(attr);
            return connectionInfo;
        }

        [FunctionName("SingleDraftCreateLobby")]
        public async Task<IActionResult> Create(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single")] HttpRequest req
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            var id = Guid.NewGuid();
            token.Add("id", id);
            await collector.AddAsync(token);
            return new CreatedResult($"/single/{id}", new { id });
        }

        [FunctionName("SingleDraftSaveLobby")]
        public async Task<IActionResult> Save(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "single/{id}")] HttpRequest req,
            string id,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            await collector.AddAsync(token);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "update",
                Arguments = new object[] {}
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }

        [FunctionName("SingleDraftGetLobby")]
        public IActionResult Get(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")] JObject document,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "single/{id}")] HttpRequest req

        )
        {
            return new OkObjectResult(document);
        }

        [FunctionName("SingleDraftClaimSlot")]
        public async Task<IActionResult> ClaimSlot(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/claim")] HttpRequest req,
            string id,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")] JObject document,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var body = await JObject.LoadAsync(jsonReader);

            var slots = document["slots"] as JArray;
            var slot = slots.Where(item => item["slot"].Value<int>() == body["slot"].Value<int>() && item["state"].Value<int>() == 0).FirstOrDefault();
            if(slot == null)
                return new BadRequestObjectResult("Slot is already taken.");

            slot["name"] = body["name"];
            slot["state"] = 1;

            await collector.AddAsync(document);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "update",
                Arguments = new object[] {}
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }

        [FunctionName("SingleDraftReleaseSlot")]
        public async Task ReleaseSlot(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/release")] HttpRequest req,
            string id,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")] JObject document,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var body = await JObject.LoadAsync(jsonReader);

            var slots = document["slots"] as JArray;
            foreach (JObject item in slots)
            {
                if(item["slot"].Value<int>() == body["slot"].Value<int>())
                {
                    item["name"] = string.Empty;
                    item["state"] = 0;
                    item["choice"] = null;
                }
            }
            
            await collector.AddAsync(document);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "update",
                Arguments = new object[] {}
            };
            await signalRMessages.AddAsync(msg);
        }

        
        [FunctionName("SingleDraftReady")]
        public async Task<IActionResult> Ready(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/ready")] HttpRequest req,
            string id,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")] JObject document,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            document["ready"] = true;

            await collector.AddAsync(document);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "update",
                Arguments = new object[] {}
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }

        [FunctionName("SingleDraftChoice")]
        public async Task<IActionResult> Choice(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/choice")] HttpRequest req,
            string id,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{id}",
                PartitionKey = "{id}")] JObject document,
            [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var body = await JObject.LoadAsync(jsonReader);

            var slots = document["slots"] as JArray;
            var slot = slots.Where(item => item["slot"].Value<int>() == body["slot"].Value<int>()).FirstOrDefault();
        
            slot["choice"] = body["choice"];
            slot["state"] = 2;

            await collector.AddAsync(document);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "update",
                Arguments = new object[] {}
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }

        /*
        [FunctionName("SingleDraftNegotiate")]
        public async Task<SignalRConnectionInfo> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{session}/negotiate")] HttpRequest req,
            string session,
            IBinder binder)
        {
            var attr = new SignalRConnectionInfoAttribute()
            {
                ConnectionStringSetting = "SignalRConnectionString",
                HubName = "SingleDraft",
                UserId = session,
            };
            var connectionInfo = await binder.BindAsync<SignalRConnectionInfo>(attr);
            return connectionInfo;
        }

        [FunctionName("SingleDraftCreateSession")]
        public async Task<IActionResult> CreateSession(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<JObject> collector,
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/create")] HttpRequest req
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);
            await collector.AddAsync(token);
            return new OkResult();
        }

        [FunctionName("SingleDraftGetSession")]
        public IActionResult GetSession(
             [CosmosDB(
                databaseName: "hgv-aquila",
                collectionName: "single-draft",
                ConnectionStringSetting = "CosmosDBConnectionString",
                Id = "{session}",
                PartitionKey = "{session}")] JObject document,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "single/get/{session}")] HttpRequest req
        )
        {
            return new OkObjectResult(document);
        }

        [FunctionName("SingleDraftClaimSlot")]
        public static Task ClaimSlot(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/claim")] HttpRequest req,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var serializer = JsonSerializer.CreateDefault();
            var body = serializer.Deserialize<dynamic>(jsonReader);

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = body.session,
                    Target = "SelectedSlot",
                    Arguments = new[] { body.slot }
                });
        }

        [FunctionName("SingleDraftSelecteHero")]
        public static Task SelecteHero(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/draft")] HttpRequest req,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var serializer = JsonSerializer.CreateDefault();
            var body = serializer.Deserialize<dynamic>(jsonReader);

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = body.session,
                    Target = "SelectedHero",
                    Arguments = new[] { body.slot, body.key }
                });
        }
        */
    }
}