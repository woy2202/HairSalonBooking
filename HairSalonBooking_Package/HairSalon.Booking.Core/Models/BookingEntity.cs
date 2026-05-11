namespace HairSalon.Booking.Core.Models;

public abstract class BookingEntity
{
    public string id { get; set; } = Guid.NewGuid().ToString("N");
    public string partitionKey { get; set; } = string.Empty;
}
