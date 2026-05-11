using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Api.Model;

public sealed record AppointmentRequest(
    string CustomerId,
    string HairdresserId,
    string SalonServiceId,
    DateTimeOffset StartAt,
    AppointmentStatus Status,
    string? Notes);
