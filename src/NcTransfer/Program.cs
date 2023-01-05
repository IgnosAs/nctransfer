using Ignos.Api.Client;
using IgnosCncSetupCamTransfer;
using IgnosCncSetupCamTransfer.Auth;
using IgnosCncSetupCamTransfer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Canceling...");
            cts.Cancel();
            e.Cancel = true;
        };

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                services.Configure<IgnosApiOptions>(hostContext.Configuration.GetSection(IgnosApiOptions.IgnosApi));
                services.AddIgnosHttpClient(hostContext.Configuration)
                    .AddHttpMessageHandler((serv) =>
                    {
                        IOptions<IgnosApiOptions> options = serv.GetRequiredService<IOptions<IgnosApiOptions>>();

                        return new IgnosDelegatingHandler(options.Value.ClientId, options.Value.Scope);
                    })
                    .AddDefaultPolicyHandler();

                services.AddIgnosApiClient<ICncFileTransferClient, CncFileTransferClient>();
                services.AddTransient<NcTransferService>();
            })
            .Build();

        var serv = host.Services.GetRequiredService<NcTransferService>();
        await serv.Run(args, cts.Token);
    }
}