namespace HairSalon.Booking.Api.Options;

public sealed class AzureBookingOptions
{
    public CosmosOptions Cosmos { get; set; } = new();
    public StorageOptions Storage { get; set; } = new();
}

public sealed class CosmosOptions
{
    public string? ConnectionString { get; set; }
    public string DatabaseName { get; set; } = "booking-db";
    public string ContainerName { get; set; } = "booking-items";
}

public sealed class StorageOptions
{
    public string? ConnectionString { get; set; }
    public string AppointmentBlobContainer { get; set; } = "appointment-confirmations";
    public string HairdresserPhotoBlobContainer { get; set; } = "hairdresser-photos";
    public string SalonPhotoBlobContainer { get; set; } = "salon-photos";
    public string AppointmentQueueName { get; set; } = "appointment-booked";
}
