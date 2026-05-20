namespace HairSalon.Booking.Core.Models
{
    public sealed class Review : BookingEntity
    {
        public Review()
        {
            partitionKey = nameof(Review);
        }

        public string CustomerId { get; set; } = string.Empty;
        public string? AppointmentId { get; set; }
        public string? HairdresserId { get; set; }
        public string? SalonServiceId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
