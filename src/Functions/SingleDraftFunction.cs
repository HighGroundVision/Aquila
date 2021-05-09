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

        [FunctionName("SingleDraftJoin")]
        public async Task<IActionResult> Join(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/join")] HttpRequest req,
            string id,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "join",
                Arguments = new object[] { token["player_id"], token["player_name"] }
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }

        [FunctionName("SingleDraftClaim")]
        public async Task<IActionResult> Claim(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/claim")] HttpRequest req,
            string id,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "claim",
                Arguments = new object[] { token["player_id"], token["slot_id"], token["selection"], token["choice"] }
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }

        [FunctionName("SingleDraftChoice")]
        public async Task<IActionResult> Choice(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/choice")] HttpRequest req,
            string id,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "choice",
                Arguments = new object[] { token["slot_id"], token["choice"]  }
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }


        [FunctionName("SingleDraftPick")]
        public async Task<IActionResult> Pick(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/pick")] HttpRequest req,
            string id,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "pick",
                Arguments = new object[] { token["slot_id"], token["choice"]  }
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }

        [FunctionName("SingleDraftKick")]
        public async Task<IActionResult> Kick(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{id}/kick")] HttpRequest req,
            string id,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var token = await JObject.LoadAsync(jsonReader);

            var msg = new SignalRMessage
            {
                UserId = id,
                Target = "kick",
                Arguments = new object[] {  token["player_id"] }
            };
            await signalRMessages.AddAsync(msg);

            return new OkResult();
        }
    }
}