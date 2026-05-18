using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Patterns;
using HairSalon.Booking.Core.Repositories;

namespace HairSalon.Booking.Core.Services
{
    public sealed class AppointmentBookingFacade : IAppointmentBookingFacade
    {
        private static readonly TimeOnly SalonOpeningTime = new TimeOnly(9, 0);
        private static readonly TimeOnly SalonClosingTime = new TimeOnly(17, 0);
        private const int SlotMinutes = 30;

        private readonly IBookingRepository<Customer> _customers;
        private readonly IBookingRepository<Hairdresser> _hairdressers;
        private readonly IBookingRepository<SalonService> _services;
        private readonly IBookingRepository<Appointment> _appointments;
        private readonly IBookingEventPublisher _eventPublisher;

        public AppointmentBookingFacade(
            IBookingRepository<Customer> customers,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services,
            IBookingRepository<Appointment> appointments,
            IBookingEventPublisher eventPublisher)
        {
            _customers = customers;
            _hairdressers = hairdressers;
            _services = services;
            _appointments = appointments;
            _eventPublisher = eventPublisher;
        }

        public async Task<Appointment> BookAsync(Appointment appointment, CancellationToken cancellationToken)
        {
            var customer = await _customers.GetAsync(appointment.CustomerId, cancellationToken);
            var hairdresser = await _hairdressers.GetAsync(appointment.HairdresserId, cancellationToken);
            var service = await _services.GetAsync(appointment.SalonServiceId, cancellationToken);

            if (customer is null)
            {
                throw new InvalidOperationException("Klient nie istnieje.");
            }

            if (hairdresser is null || !hairdresser.IsActive)
            {
                throw new InvalidOperationException("Fryzjer nie istnieje albo jest nieaktywny.");
            }

            if (service is null || !service.IsAvailable)
            {
                throw new InvalidOperationException("Usługa salonu nie istnieje albo jest niedostępna.");
            }

            appointment.EndAt = appointment.StartAt.AddMinutes(service.DurationMinutes);
            appointment.Status = AppointmentStatus.Booked;
            ValidateAppointmentTime(appointment);
            await EnsureSlotIsFreeAsync(appointment, cancellationToken);

            var created = await _appointments.CreateAsync(appointment, cancellationToken);
            await _eventPublisher.PublishAppointmentBookedAsync(created, cancellationToken);
            return created;
        }

        public async Task<IReadOnlyList<DateTimeOffset>> GetAvailableSlotsAsync(string hairdresserId, DateOnly day, CancellationToken cancellationToken)
        {
            var utcDay = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var allSlots = new AvailabilitySlotCollection(utcDay, SalonOpeningTime, SalonClosingTime, SlotMinutes);
            var existing = await _appointments.GetAllAsync(cancellationToken);

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
            var allAppointments = await _appointments.GetAllAsync(cancellationToken);
            var hasCollision = allAppointments.Any(existing =>
                existing.HairdresserId == requested.HairdresserId &&
                existing.Status != AppointmentStatus.Cancelled &&
                requested.StartAt < existing.EndAt &&
                requested.EndAt > existing.StartAt);

            if (hasCollision)
            {
                throw new InvalidOperationException("Wybrany termin wizyty jest już zajęty.");
            }
        }

        private static void ValidateAppointmentTime(Appointment appointment)
        {
            if (appointment.StartAt == default)
            {
                throw new InvalidOperationException("Data rozpoczecia wizyty jest wymagana.");
            }

            if (appointment.StartAt.ToUniversalTime() <= DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("Nie mozna utworzyc wizyty w przeszlosci.");
            }

            if (appointment.StartAt.Minute % SlotMinutes != 0 || appointment.StartAt.Second != 0)
            {
                throw new InvalidOperationException("Wizyta musi zaczynac sie o pelnej polgodzinie, np. 09:00 albo 09:30.");
            }

            var startTime = TimeOnly.FromDateTime(appointment.StartAt.DateTime);
            var endTime = TimeOnly.FromDateTime(appointment.EndAt.DateTime);

            if (startTime < SalonOpeningTime || endTime > SalonClosingTime || appointment.EndAt.Date != appointment.StartAt.Date)
            {
                throw new InvalidOperationException("Wizyta musi miescic sie w godzinach pracy salonu od 09:00 do 17:00.");
            }
        }
    }
}
