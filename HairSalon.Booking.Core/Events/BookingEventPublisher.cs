using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Events;

// Observer/Event pattern: storage, queues and future subscribers react to bookings independently.
public interface IBookingEventPublisher
{
    event EventHandler<AppointmentBookedEventArgs>? AppointmentBooked;
    Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken);
}

public sealed class BookingEventPublisher(IEnumerable<IAppointmentBookedHandler> handlers) : IBookingEventPublisher
{
    public event EventHandler<AppointmentBookedEventArgs>? AppointmentBooked;

    public async Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        AppointmentBooked?.Invoke(this, new AppointmentBookedEventArgs(appointment));

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(appointment, cancellationToken);
        }
    }
}
