namespace HairSalon.Booking.Api.Model
{
    public sealed class ReviewRequest
    {
        public string? AppointmentId { get; set; }
        public string? HairdresserId { get; set; }
        public string? SalonServiceId { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
