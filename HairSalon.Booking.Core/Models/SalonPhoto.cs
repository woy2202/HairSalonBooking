namespace HairSalon.Booking.Core.Models
{
    public sealed class SalonPhoto : BookingEntity
    {
        public SalonPhoto()
        {
            partitionKey = nameof(SalonPhoto);
        }

        public string FileName { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
        public string BlobUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayWidth { get; set; } = 800;
        public int DisplayHeight { get; set; } = 800;
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
