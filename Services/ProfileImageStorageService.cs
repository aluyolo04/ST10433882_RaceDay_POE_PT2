
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.Api.Services;

public class ProfileImageStorageService
{
    private readonly BlobContainerClient _container;

    public ProfileImageStorageService(IConfiguration configuration)
    {
        var connectionString =
            configuration["AzureStorage:ConnectionString"];

        var containerName =
            configuration["AzureStorage:ContainerName"]
            ?? "profile-images";

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Azure Storage connection string is missing.");

        _container = new BlobContainerClient(
            connectionString, containerName);
    }

    public async Task<string> UploadAsync(
        Stream stream, string contentType, string extension)
    {
        await _container.CreateIfNotExistsAsync(
            PublicAccessType.None);

        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blob = _container.GetBlobClient(blobName);

        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        });

        return blobName;
    }

    public Uri GetReadUrl(string blobName)
    {
        var blob = _container.GetBlobClient(blobName);

        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException(
                "Shared-key SAS generation is unavailable.");

        return blob.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.AddMinutes(30));
    }

    public async Task DeleteAsync(string blobName)
    {
        await _container.GetBlobClient(blobName)
            .DeleteIfExistsAsync();
    }
}
