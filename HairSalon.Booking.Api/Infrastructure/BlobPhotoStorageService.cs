using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HairSalon.Booking.Api.Options;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Infrastructure;

public sealed class BlobPhotoStorageService(IOptions<AzureBookingOptions> options) : IPhotoStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public async Task<UploadedPhoto> UploadAsync(string containerName, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Only JPG, PNG and WEBP images are supported.");
        }

        var connectionString = options.Value.Storage.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is not configured.");
        }

        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: cancellationToken);

        return new UploadedPhoto(file.FileName, blobName, blob.Uri.ToString());
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var connectionString = options.Value.Storage.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var container = new BlobContainerClient(connectionString, containerName);
        await container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }
}
