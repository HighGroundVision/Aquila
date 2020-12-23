using HGV.Aquila.Models;
using HGV.Basilius.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Registry;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HGV.Aquila.Functions
{
    public class HeroesFunction
    {
        private readonly IMetaClient metaClient;
        private readonly HttpClient httpClient;
        private readonly IAsyncPolicy<string> cachePolicy;
        private readonly string statsUrl;

        public HeroesFunction(HttpClient httpClient, IMetaClient metaClient, IReadOnlyPolicyRegistry<string> policyRegistry)
        {
            this.httpClient = httpClient;
            this.metaClient = metaClient;
            this.cachePolicy = policyRegistry.Get<IAsyncPolicy<string>>("GetHeroes");
            this.statsUrl = Environment.GetEnvironmentVariable("StatsUrl");
        }

        [FunctionName("Heroes")]
        public async Task<IActionResult> GetHeroes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "heroes")] HttpRequest req,
            ILogger log)
        { 
            var policy = await this.cachePolicy.ExecuteAndCaptureAsync(() => this.httpClient.GetStringAsync(statsUrl));
            var result = JsonConvert.DeserializeObject<Root>(policy.Result);
            var major = result.Data.Patches.Majors.Last();
            var patch = result.Data.Patches.Details[major].FirstOrDefault();
            var stats = result.Data.Heroes.Select(_ => new { Id = _.Key, Data = _.Value[patch] }).ToList();
            var meta = this.metaClient.GetHeroes().Where(_ => _.AbilityDraftEnabled == true).ToList();

            var collection = meta
                .Join(stats, _ => _.Id, _ => _.Id, (lhs, rhs) => new Hero() 
                { 
                    Id = lhs.Id,
                    Name = lhs.Name,
                    Key = lhs.Key.Replace("npc_dota_hero_", ""),
                    Aliases = lhs.NameAliases,
                    ImageBanner = lhs.ImageBanner,
                    ImageIcon = lhs.ImageIcon,
                    ImageProfile = lhs.ImageProfile,
                    AbilityReplaceRequired = lhs.AbilityReplaceRequired,
                    StrengthGain = lhs.AttributeStrengthGain,
                    MaxStrength = lhs.AttributeBaseStrength + (lhs.AttributeStrengthGain * 30),
                    IntelligenceGain = lhs.AttributeIntelligenceGain,
                    MaxIntelligence= lhs.AttributeBaseIntelligence + (lhs.AttributeIntelligenceGain * 30),
                    AgilityGain = lhs.AttributeAgilityGain,
                    MaxAgility = lhs.AttributeBaseAgility + (lhs.AttributeAgilityGain * 30),
                    AttributePrimary = lhs.AttributePrimary,
                    AttackCapabilities = lhs.AttackCapabilities,
                    Damage = (lhs.AttackDamageMin + lhs.AttackDamageMax) / 2,
                    Roles = lhs.Roles,
                    WinRate = rhs.Data.WinRate,
                    Picked = false,
                })
                .OrderBy(_ => _.Name)
                .ToList();

            return new OkObjectResult(collection);
        }
    }
}