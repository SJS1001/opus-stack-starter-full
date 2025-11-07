using Azure.Storage.Blobs;

namespace Opus.Storage;

public sealed class BlobClientFactory
{
    private readonly BlobServiceClient _svc;
    public BlobClientFactory(string endpoint, string accountName, string accountKey)
    {
        var conn = $"DefaultEndpointsProtocol=http;AccountName={accountName};AccountKey={accountKey};BlobEndpoint={endpoint};";
        _svc = new BlobServiceClient(conn);
    }
    public BlobContainerClient GetContainer(string name) => _svc.GetBlobContainerClient(name);
}
