using System.Diagnostics;
using System.Runtime.InteropServices;
using Azure.Storage.Blobs;
using Ignos.Api.Client;

namespace NcTransfer;

internal class NcTransferService : INcTransferService
{
    private readonly ICncFileTransferClient _cncFileTransferClient;
    private readonly ILogger<NcTransferService> _logger;

    public NcTransferService(ICncFileTransferClient cncFileTransferClient, ILogger<NcTransferService> logger)
    {
        _cncFileTransferClient = cncFileTransferClient;
        _logger = logger;
    }

    public async Task Run(string[] args, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
#if DEBUG
        args = new string[] { @"C:\temp\demo.txt" };
#else
        if (args.Length== 0)
        {
            _logger.LogError("Missing file argument");
            return;
        }
        else if (args.Length > 1)
        {
            _logger.LogError("Too many parameters");
            return;
        }
#endif
        var file = args[0];
        var upload = await _cncFileTransferClient.CreateUploadCamTransferInfoAsync(new UploadCamFileRequest { Filename = Path.GetFileName(file) });
        if (cancellationToken.IsCancellationRequested)
            return;
        var blobClient = new BlobClient(new Uri(upload.UploadUrl));
        using var localFile = File.OpenRead(file);

        await blobClient.UploadAsync(localFile, true, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;

        _logger.LogInformation("After upload");

        string url = upload.Url;
        try
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch (Exception)
        {
            _logger.LogError("Could not start browser with url input.");
        }
        sw.Stop();
        _logger.LogInformation($"Total time: {sw.ElapsedMilliseconds} ms");
    }
}
