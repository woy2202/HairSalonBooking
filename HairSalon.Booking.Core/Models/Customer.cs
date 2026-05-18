namespace HairSalon.Booking.Core.Models
{
    public sealed class Customer : BookingEntity
    {
        public Customer()
        {
            partitionKey = nameof(Customer);
        }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
