namespace NcTransfer;

internal interface INcTransferService
{
    Task Run(string[] args, CancellationToken cancellationToken = default);
}
