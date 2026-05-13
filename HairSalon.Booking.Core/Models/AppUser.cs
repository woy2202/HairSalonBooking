namespace HairSalon.Booking.Core.Models;

public sealed class AppUser : BookingEntity
{
    public AppUser()
    {
        partitionKey = nameof(AppUser);
    }

    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public string? CustomerId { get; set; }
    public string? HairdresserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastLoginAt { get; set; } = DateTimeOffset.UtcNow;
}
