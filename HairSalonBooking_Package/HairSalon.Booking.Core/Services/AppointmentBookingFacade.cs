using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Patterns;
using HairSalon.Booking.Core.Repositories;

namespace HairSalon.Booking.Core.Services;

// Facade: one high-level booking operation coordinates validation, persistence and Azure events.
public sealed class AppointmentBookingFacade(
    IBookingRepository<Customer> customers,
    IBookingRepository<Hairdresser> hairdressers,
    IBookingRepository<SalonService> services,
    IBookingRepository<Appointment> appointments,
    IBookingEventPublisher eventPublisher) : IAppointmentBookingFacade
{
    public async Task<Appointment> BookAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var customer = await customers.GetAsync(appointment.CustomerId, cancellationToken);
        var hairdresser = await hairdressers.GetAsync(appointment.HairdresserId, cancellationToken);
        var service = await services.GetAsync(appointment.SalonServiceId, cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException("Customer does not exist.");
        }

        if (hairdresser is null || !hairdresser.IsActive)
        {
            throw new InvalidOperationException("Hairdresser does not exist or is not active.");
        }

        if (service is null || !service.IsAvailable)
        {
            throw new InvalidOperationException("Salon service does not exist or is not available.");
        }

        appointment.EndAt = appointment.StartAt.AddMinutes(service.DurationMinutes);
        await EnsureSlotIsFreeAsync(appointment, cancellationToken);

        var created = await appointments.CreateAsync(appointment, cancellationToken);
        await eventPublisher.PublishAppointmentBookedAsync(created, cancellationToken);
        return created;
    }

    public async Task<IReadOnlyList<DateTimeOffset>> GetAvailableSlotsAsync(string hairdresserId, DateOnly day, CancellationToken cancellationToken)
    {
        var utcDay = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var allSlots = new AvailabilitySlotCollection(utcDay, new TimeOnly(9, 0), new TimeOnly(17, 0), 30);
        var existing = await appointments.GetAllAsync(cancellationToken);

        return allSlots
            .Where(slot => existing.All(appointment =>
                appointment.HairdresserId != hairdresserId ||
                appointment.Status == AppointmentStatus.Cancelled ||
                slot < appointment.StartAt ||
                slot >= appointment.EndAt))
            .ToList();
    }

    private async Task EnsureSlotIsFreeAsync(Appointment requested, CancellationToken cancellationToken)
    {
        var allAppointments = await appointments.GetAllAsync(cancellationToken);
        var hasCollision = allAppointments.Any(existing =>
            existing.HairdresserId == requested.HairdresserId &&
            existing.Status != AppointmentStatus.Cancelled &&
            requested.StartAt < existing.EndAt &&
            requested.EndAt > existing.StartAt);

        if (hasCollision)
        {
            throw new InvalidOperationException("Selected appointment slot is already booked.");
        }
    }
}
