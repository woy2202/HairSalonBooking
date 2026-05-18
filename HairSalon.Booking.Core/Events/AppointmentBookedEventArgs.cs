using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Events
{
    public sealed class AppointmentBookedEventArgs : EventArgs
    {
        public AppointmentBookedEventArgs(Appointment appointment)
        {
            Appointment = appointment;
        }

        public Appointment Appointment { get; }
    }
}
