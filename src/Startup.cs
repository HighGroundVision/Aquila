using HGV.Basilius.Client;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Caching;
using Polly.Registry;
using System;

[assembly: FunctionsStartup(typeof(HGV.Aquila.Startup))]

namespace HGV.Aquila
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Singleton
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IMetaClient, MetaClient>();
        }
    }
}
