namespace HairSalon.Booking.Core.Models
{
    public sealed class SalonService : BookingEntity
    {
        public SalonService()
        {
            partitionKey = nameof(SalonService);
        }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
