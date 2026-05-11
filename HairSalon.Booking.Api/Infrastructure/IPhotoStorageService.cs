namespace HairSalon.Booking.Api.Infrastructure;

public sealed record UploadedPhoto(string FileName, string BlobName, string Url);

public interface IPhotoStorageService
{
    Task<UploadedPhoto> UploadAsync(string containerName, IFormFile file, CancellationToken cancellationToken);
    Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken);
}
