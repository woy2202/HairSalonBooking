namespace HairSalon.Booking.Api.Model;

public sealed record HairdresserRequest(
    string FirstName, 
    string LastName, 
    string Specialization, 
    bool IsActive);
