namespace HairSalon.Booking.Core.Models;

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
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
