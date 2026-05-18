namespace HairSalon.Booking.Api.Model
{
    public sealed class HairdresserRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
