namespace HairSalon.Booking.Api.Model;

public sealed record CustomerRequest(
    string FirstName, 
    string LastName, 
    string PhoneNumber, 
    string Email, 
    string? Notes);
