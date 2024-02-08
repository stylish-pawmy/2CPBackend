namespace Eventi.Server.Services;

using Azure.Storage.Blobs;

public class AzureBlobStorage : IBlobStorage
{
    private readonly IConfiguration _configuration;

    public AzureBlobStorage(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> UploadBlobAsync(string containerName, string fileName, IFormFile file)
    {
        BlobContainerClient container = new BlobContainerClient(
                _configuration.GetConnectionString("azureStore"),
                containerName
            );

        BlobClient blob = container.GetBlobClient(fileName);

        using (Stream stream = file.OpenReadStream())
        {
            await blob.UploadAsync(stream);
        }
        
        return blob.Uri.AbsoluteUri;
    }

    public async Task DeleteBlobAsync(string containerName, string fileName)
    {
        BlobContainerClient container = new BlobContainerClient(
            _configuration.GetConnectionString("azureStore"),
            containerName);
        
        BlobClient blob = container.GetBlobClient(fileName);

        await blob.DeleteIfExistsAsync();
    }
}