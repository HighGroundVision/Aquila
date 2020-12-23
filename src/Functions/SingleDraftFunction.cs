using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/{userId}/negotiate")] HttpRequest req,
            string userId,
            IBinder binder)
        {
            var attr = new SignalRConnectionInfoAttribute()
            {
                ConnectionStringSetting = "SignalConnectionString",
                HubName = "SingleDraft",
                UserId = userId,
            };
            var connectionInfo = await binder.BindAsync<SignalRConnectionInfo>(attr);
            return connectionInfo;
        }

        [FunctionName("SingleDraftJoinLobby")]
        public Task JoinLobby(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/join")]HttpRequest req,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalConnectionString")]IAsyncCollector<SignalRGroupAction> groupActions)
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var serializer = JsonSerializer.CreateDefault();
            var body = serializer.Deserialize<dynamic>(jsonReader);
            return groupActions.AddAsync(
                new SignalRGroupAction
                {
                    UserId = body.UserId,
                    GroupName = body.GroupName,
                    Action = GroupAction.Add
                });
        }

        [FunctionName("SingleDraftDestroyLobby")]
        public Task DestroyLobby(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/destroy")]HttpRequest req,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalConnectionString")]IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var serializer = JsonSerializer.CreateDefault();
            var body = serializer.Deserialize<dynamic>(jsonReader);

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    GroupName = body.GroupName,
                    Target = "lobbyDestroyed",
                    Arguments = new [] { body.GroupName }
                });
        }

        [FunctionName("SingleDraft")]
        public static Task SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "single/message")]HttpRequest req,
            [SignalR(HubName = "SingleDraft", ConnectionStringSetting = "SignalConnectionString")]IAsyncCollector<SignalRMessage> signalRMessages
        )
        {
            var textReader = new StreamReader(req.Body);
            var jsonReader = new JsonTextReader(textReader);
            var serializer = JsonSerializer.CreateDefault();
            var body = serializer.Deserialize<dynamic>(jsonReader);

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    GroupName = body.GroupName,
                    Target = "newMessage",
                    Arguments = new [] { body.Username, body.Message }
                });
        }
    }
}