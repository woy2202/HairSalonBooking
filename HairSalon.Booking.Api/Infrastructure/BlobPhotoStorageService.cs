using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HairSalon.Booking.Api.Options;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class BlobPhotoStorageService : IPhotoStorageService
    {
        private static readonly HashSet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly IOptions<AzureBookingOptions> _options;

        public BlobPhotoStorageService(IOptions<AzureBookingOptions> options)
        {
            _options = options;
        }

        public async Task<UploadedPhoto> UploadAsync(string containerName, IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                throw new InvalidOperationException("Przesłany plik jest pusty.");
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                throw new InvalidOperationException("Obsługiwane są tylko zdjęcia JPG, PNG oraz WEBP.");
            }

            var maxFileSizeBytes = _options.Value.Photos.MaxFileSizeMb * 1024L * 1024L;
            if (file.Length > maxFileSizeBytes)
            {
                throw new InvalidOperationException($"Zdjęcie jest za duże. Maksymalny rozmiar pliku to {_options.Value.Photos.MaxFileSizeMb} MB.");
            }

            var connectionString = _options.Value.Storage.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Brakuje konfiguracji Azure Storage.");
            }

            var extension = GetExtension(file.ContentType);
            var blobName = $"{DateTimeOffset.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
            var container = new BlobContainerClient(connectionString, containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blob = container.GetBlobClient(blobName);
            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType,
                        CacheControl = "public, max-age=86400"
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["displayWidth"] = _options.Value.Photos.DisplayWidth.ToString(),
                        ["displayHeight"] = _options.Value.Photos.DisplayHeight.ToString(),
                        ["displayMode"] = "cover"
                    }
                },
                cancellationToken);

            return new UploadedPhoto(
                file.FileName,
                blobName,
                blob.Uri.ToString(),
                _options.Value.Photos.DisplayWidth,
                _options.Value.Photos.DisplayHeight);
        }

        public async Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            var connectionString = _options.Value.Storage.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            var container = new BlobContainerClient(connectionString, containerName);
            await container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
        }

        private static string GetExtension(string contentType)
        {
            switch (contentType.ToLowerInvariant())
            {
                case "image/jpeg":
                    return ".jpg";
                case "image/png":
                    return ".png";
                case "image/webp":
                    return ".webp";
                default:
                    return ".img";
            }
        }
    }
}
