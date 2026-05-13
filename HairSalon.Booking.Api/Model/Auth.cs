namespace HairSalon.Booking.Api.Model;

public sealed record AuthProfileRequest(
    string? DisplayName, 
    string? Email);
