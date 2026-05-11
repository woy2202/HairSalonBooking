namespace HairSalon.Booking.Api.Contracts;

public sealed record CustomerRequest(string FirstName, string LastName, string PhoneNumber, string Email, string? Notes);
