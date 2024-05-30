using Ignos.Api.Client;
using Microsoft.Extensions.Options;
using NcTransfer;
using NcTransfer.Auth;
using NcTransfer.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog(loggerConfiguration => loggerConfiguration.ReadFrom.Configuration(builder.Configuration));
builder.Services.Configure<IgnosApiOptions>(builder.Configuration.GetSection(IgnosApiOptions.IgnosApi));
builder.Services.AddIgnosHttpClient(builder.Configuration)
    .AddHttpMessageHandler((serv) =>
    {
        IOptions<IgnosApiOptions> options = serv.GetRequiredService<IOptions<IgnosApiOptions>>();

        return new IgnosDelegatingHandler(options.Value.TenantId, options.Value.ClientId, options.Value.Scope);
    })
    .AddDefaultPolicyHandler();

builder.Services.AddIgnosApiClient<ICncFileTransferClient, CncFileTransferClient>();
builder.Services.AddIgnosApiClient<IUploadClient, UploadClient>();
builder.Services.AddTransient<NcTransferService>();
builder.Services.AddTransient<NcPostFileService>();

builder.Services.AddTransient<INcTransferService>((sp) => 
    args switch 
    { 
        { Length: 1 } => sp.GetRequiredService<NcTransferService>(),
        { Length: 2 } => sp.GetRequiredService<NcPostFileService>(),
        _ => throw new Exception($"Usage: {Environment.GetCommandLineArgs()[0]} file [machineId]")
    });


using IHost host = builder.Build();
await host.StartAsync();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

var serv = host.Services.GetRequiredService<INcTransferService>();
await serv.Run(args, lifetime.ApplicationStopping);

lifetime.StopApplication();
await host.WaitForShutdownAsync();
