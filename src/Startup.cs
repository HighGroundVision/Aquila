using HGV.Basilius.Client;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Caching;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

[assembly: FunctionsStartup(typeof(HGV.Aquila.Startup))]

namespace HGV.Aquila
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Singleton
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IMetaClient>(new MetaClient());
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<Polly.Caching.IAsyncCacheProvider, Polly.Caching.Memory.MemoryCacheProvider>();

            builder.Services.AddSingleton<Polly.Registry.IReadOnlyPolicyRegistry<string>, Polly.Registry.PolicyRegistry>((serviceProvider) =>
            {
                var registry = new PolicyRegistry();
                var provider = serviceProvider.GetRequiredService<IAsyncCacheProvider>().AsyncFor<string>();
                registry.Add("GetHeroes", Policy.CacheAsync<string>(provider,TimeSpan.FromMinutes(5)));
                return registry;
            });
        }
    }
}
