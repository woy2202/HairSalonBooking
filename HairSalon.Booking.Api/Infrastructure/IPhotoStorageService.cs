namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class UploadedPhoto
    {
        public UploadedPhoto(string fileName, string blobName, string url, int displayWidth, int displayHeight)
        {
            FileName = fileName;
            BlobName = blobName;
            Url = url;
            DisplayWidth = displayWidth;
            DisplayHeight = displayHeight;
        }

        public string FileName { get; }

        public string BlobName { get; }

        public string Url { get; }

        public int DisplayWidth { get; }

        public int DisplayHeight { get; }
    }

    public interface IPhotoStorageService
    {
        Task<UploadedPhoto> UploadAsync(string containerName, IFormFile file, CancellationToken cancellationToken);
        Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken);
    }
}
