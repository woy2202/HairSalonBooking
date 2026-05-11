using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Api.Contracts;

public sealed record AppointmentRequest(
    string CustomerId,
    string HairdresserId,
    string SalonServiceId,
    DateTimeOffset StartAt,
    AppointmentStatus Status,
    string? Notes);
