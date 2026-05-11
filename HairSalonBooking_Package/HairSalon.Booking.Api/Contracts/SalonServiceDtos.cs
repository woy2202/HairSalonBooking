namespace HairSalon.Booking.Api.Contracts;

public sealed record SalonServiceRequest(string Name, string Description, int DurationMinutes, decimal Price, bool IsAvailable);
