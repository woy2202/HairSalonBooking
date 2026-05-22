using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Api.Model
{
    public sealed class AssignUserRoleRequest
    {
        public UserRole Role { get; set; }

        public string? Specialization { get; set; }

        public string? HairdresserId { get; set; }
    }
}
