namespace HairSalon.Booking.Api.Model;

public sealed record SalonServiceRequest(
    string Name, 
    string Description, 
    int DurationMinutes, 
    decimal Price, 
    bool IsAvailable);
