using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Services;

public interface IAppointmentBookingFacade
{
    Task<Appointment> BookAsync(Appointment appointment, CancellationToken cancellationToken);
    Task<IReadOnlyList<DateTimeOffset>> GetAvailableSlotsAsync(string hairdresserId, DateOnly day, CancellationToken cancellationToken);
}
