namespace HairSalon.Booking.Core.Models
{
    public sealed class Hairdresser : BookingEntity
    {
        public Hairdresser()
        {
            partitionKey = nameof(Hairdresser);
        }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? PhotoBlobName { get; set; }
        public int PhotoDisplayWidth { get; set; } = 800;
        public int PhotoDisplayHeight { get; set; } = 800;
        public bool IsActive { get; set; } = true;
    }
}
