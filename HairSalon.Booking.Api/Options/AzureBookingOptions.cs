namespace HairSalon.Booking.Api.Options
{
    public sealed class AzureBookingOptions
    {
        public CosmosOptions Cosmos { get; set; } = new CosmosOptions();
        public StorageOptions Storage { get; set; } = new StorageOptions();
        public PhotoOptions Photos { get; set; } = new PhotoOptions();
        public SignalROptions SignalR { get; set; } = new SignalROptions();
        public KeyVaultOptions KeyVault { get; set; } = new KeyVaultOptions();
        public CorsOptions Cors { get; set; } = new CorsOptions();
        public SecurityOptions Security { get; set; } = new SecurityOptions();
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
        public string HairdresserPhotoBlobContainer { get; set; } = "fryzjerzy-photo";
        public string SalonPhotoBlobContainer { get; set; } = "salon-photo";
        public string AppointmentQueueName { get; set; } = "booking-notifications";
    }

    public sealed class PhotoOptions
    {
        public int MaxFileSizeMb { get; set; } = 5;
        public int DisplayWidth { get; set; } = 800;
        public int DisplayHeight { get; set; } = 800;
    }

    public sealed class SignalROptions
    {
        public string? ConnectionString { get; set; }
    }

    public sealed class KeyVaultOptions
    {
        public string? VaultUri { get; set; }
    }

    public sealed class CorsOptions
    {
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    }

    public sealed class SecurityOptions
    {
        public bool RequireEasyAuthHeaders { get; set; } = true;
    }
}
