using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Api.Model
{
    public sealed class AppointmentRequest
    {
        public string CustomerId { get; set; } = string.Empty;

        public string HairdresserId { get; set; } = string.Empty;

        public string SalonServiceId { get; set; } = string.Empty;

        public DateTimeOffset StartAt { get; set; }

        public AppointmentStatus Status { get; set; }

        public string? Notes { get; set; }
    }

    public sealed class CreateAppointmentRequest
    {
        public string CustomerId { get; set; } = string.Empty;

        public string HairdresserId { get; set; } = string.Empty;

        public string SalonServiceId { get; set; } = string.Empty;

        public DateTimeOffset StartAt { get; set; }

        public string? Notes { get; set; }
    }

    public sealed class ChangeAppointmentStatusRequest
    {
        public AppointmentStatus Status { get; set; }
    }
}
