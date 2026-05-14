using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Api.Model;

public sealed record AssignUserRoleRequest(
    UserRole Role,
    string? CustomerId,
    string? HairdresserId);
