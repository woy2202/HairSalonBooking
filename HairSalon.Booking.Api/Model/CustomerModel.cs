namespace HairSalon.Booking.Api.Model
{
    public sealed class CustomerRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}
