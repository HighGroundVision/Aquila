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
    }
}