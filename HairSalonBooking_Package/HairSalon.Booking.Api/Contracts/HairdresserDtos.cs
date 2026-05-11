namespace HairSalon.Booking.Api.Contracts;

public sealed record HairdresserRequest(string FirstName, string LastName, string Specialization, bool IsActive);
