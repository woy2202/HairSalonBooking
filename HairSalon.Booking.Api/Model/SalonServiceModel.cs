namespace HairSalon.Booking.Api.Model
{
    public sealed class SalonServiceRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }
    }
}
