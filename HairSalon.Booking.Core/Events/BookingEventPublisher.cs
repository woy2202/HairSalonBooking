using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Events
{
    public interface IBookingEventPublisher
    {
        event EventHandler<AppointmentBookedEventArgs>? AppointmentBooked;
        Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken);
    }

    public sealed class BookingEventPublisher : IBookingEventPublisher
    {
        private readonly IEnumerable<IAppointmentBookedHandler> _handlers;

        public BookingEventPublisher(IEnumerable<IAppointmentBookedHandler> handlers)
        {
            _handlers = handlers;
        }

        public event EventHandler<AppointmentBookedEventArgs>? AppointmentBooked;

        public async Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken)
        {
            if (AppointmentBooked != null)
            {
                AppointmentBooked(this, new AppointmentBookedEventArgs(appointment));
            }

            foreach (var handler in _handlers)
            {
                await handler.HandleAsync(appointment, cancellationToken);
            }
        }
    }
}
