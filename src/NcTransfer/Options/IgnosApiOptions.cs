namespace NcTransfer.Options;

internal class IgnosApiOptions
{
    public const string IgnosApi = "IgnosApi";
    public required string ClientId { get; set; }    
    public required string TenantId { get; set; }
    public required string Scope { get; set; }
    public required string BaseUrl { get; set; }
}
