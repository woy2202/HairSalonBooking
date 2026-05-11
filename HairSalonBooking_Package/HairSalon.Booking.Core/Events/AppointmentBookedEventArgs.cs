using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Events;

public sealed class AppointmentBookedEventArgs(Appointment appointment) : EventArgs
{
    public Appointment Appointment { get; } = appointment;
}
