namespace HairSalon.Booking.Api.Options;

public sealed class AzureBookingOptions
{
    public CosmosOptions Cosmos { get; set; } = new();
    public StorageOptions Storage { get; set; } = new();
    public SignalROptions SignalR { get; set; } = new();
    public KeyVaultOptions KeyVault { get; set; } = new();
}

public sealed class CosmosOptions
{
    public string? ConnectionString { get; set; }
    public string DatabaseName { get; set; } = "BookingApkaDB";
    public string CustomersContainerName { get; set; } = "customers";
    public string HairdressersContainerName { get; set; } = "barbers";
    public string SalonServicesContainerName { get; set; } = "services";
    public string AppointmentsContainerName { get; set; } = "appointments";
    public string SalonPhotosContainerName { get; set; } = "salon-photos";
    public string UsersContainerName { get; set; } = "users";
    public string PartitionKeyPath { get; set; } = "/id";
}

public sealed class StorageOptions
{
    public string? ConnectionString { get; set; }
    public string AppointmentBlobContainer { get; set; } = "wizyty";
    public string HairdresserPhotoBlobContainer { get; set; } = "fryzjerzy-photo";
    public string SalonPhotoBlobContainer { get; set; } = "salon-photo";
    public string AppointmentQueueName { get; set; } = "booking-notifications";
}

public sealed class SignalROptions
{
    public string? ConnectionString { get; set; }
}

public sealed class KeyVaultOptions
{
    public string? VaultUri { get; set; }
}
