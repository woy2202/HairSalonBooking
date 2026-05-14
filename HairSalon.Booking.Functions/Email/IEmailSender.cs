namespace HairSalon.Booking.Functions.Email;

public interface IEmailSender
{
    Task SendAppointmentConfirmationAsync(AppointmentBookedMessage message, CancellationToken cancellationToken);
}
