namespace Eventi.Server.Services;

public interface IBlobStorage
{
    public Task<string> UploadBlobAsync(string containerName, string fileName, IFormFile blob);
    public Task DeleteBlobAsync(string container, string fileName);
}