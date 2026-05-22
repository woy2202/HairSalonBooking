namespace HairSalon.Booking.Core.Models
{
    public sealed class Appointment : BookingEntity
    {
        public Appointment()
        {
            partitionKey = nameof(Appointment);
        }

        public string CustomerId { get; set; } = string.Empty;
        public string HairdresserId { get; set; } = string.Empty;
        public string SalonServiceId { get; set; } = string.Empty;
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
        public string? Notes { get; set; }
        public DateTimeOffset? ReminderOneDayBeforeSentAt { get; set; }
    }
}
