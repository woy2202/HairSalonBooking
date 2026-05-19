using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;

namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class AppointmentStatusService : IAppointmentStatusService
    {
        private readonly IBookingRepository<Appointment> _appointments;

        public AppointmentStatusService(IBookingRepository<Appointment> appointments)
        {
            _appointments = appointments;
        }

        public async Task RefreshExpiredAppointmentsAsync(CancellationToken cancellationToken)
        {
            var appointments = await _appointments.GetAllAsync(cancellationToken);
            foreach (var appointment in appointments)
            {
                await RefreshExpiredAppointmentAsync(appointment, cancellationToken);
            }
        }

        public async Task<Appointment> RefreshExpiredAppointmentAsync(Appointment appointment, CancellationToken cancellationToken)
        {
            if (!ShouldCompleteAutomatically(appointment))
            {
                return appointment;
            }

            appointment.Status = AppointmentStatus.Completed;
            return await _appointments.UpsertAsync(appointment, cancellationToken);
        }

        private static bool ShouldCompleteAutomatically(Appointment appointment)
        {
            var canBeCompleted = appointment.Status == AppointmentStatus.Booked ||
                appointment.Status == AppointmentStatus.Confirmed;

            return canBeCompleted && appointment.EndAt.ToUniversalTime() <= DateTimeOffset.UtcNow;
        }
    }
}
