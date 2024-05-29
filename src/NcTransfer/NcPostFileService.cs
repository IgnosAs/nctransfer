using System.Diagnostics;
using Azure.Storage.Blobs;
using Ignos.Api.Client;
using Microsoft.Extensions.Logging;

namespace NcTransfer;

internal class NcPostFileService : INcTransferService
{
    private readonly ICncFileTransferClient _cncFileTransferClient;
    private readonly IUploadClient _uploadClient;
    private readonly ILogger<NcPostFileService> _logger;

    public NcPostFileService(ICncFileTransferClient cncFileTransferClient, IUploadClient uploadClient, ILogger<NcPostFileService> logger)
    {
        _cncFileTransferClient = cncFileTransferClient;
        _uploadClient = uploadClient;
        _logger = logger;
    }

    public async Task Run(string[] args, CancellationToken cancellationToken = default)
    {
        var file = args[0];
        var upload = await _uploadClient.CreateUploadInfoAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;
        var fileName = Path.GetFileName(file);
        var blobClient = new BlobClient(new Uri($"{upload.BaseUrl}/{fileName}{upload.AccessKey}"));
        using var localFile = File.OpenRead(file);

        await blobClient.UploadAsync(localFile, true, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;

        _logger.LogInformation("After upload");

        var transfer = await _cncFileTransferClient.StartCamTransferToMachineFromTempUploadAsync(
            new StartCamTransferToMachineFromTempUpload
            {
                UploadKey = upload.Key,
                Filename = fileName,
                CncMachineId = args[1],
            }, cancellationToken);

        _logger.LogInformation($"Transfer of {file} to CNC machine {transfer.CncMachineName} started - transfer id {transfer.Id}");
    }
}
