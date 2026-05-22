namespace HairSalon.Booking.Functions.Email
{
    public interface IEmailSender
    {
        Task SendAppointmentConfirmationAsync(AppointmentBookedMessage message, CancellationToken cancellationToken);
        Task<bool> SendAppointmentReminderAsync(AppointmentReminderMessage message, CancellationToken cancellationToken);
    }
}
