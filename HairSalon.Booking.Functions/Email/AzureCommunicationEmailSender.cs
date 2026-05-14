using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Functions.Email;

public sealed class AzureCommunicationEmailSender(
    IOptions<EmailOptions> options,
    ILogger<AzureCommunicationEmailSender> logger) : IEmailSender
{
    public async Task SendAppointmentConfirmationAsync(AppointmentBookedMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.CustomerEmail))
        {
            logger.LogWarning("Appointment {AppointmentId} has no customer email. Confirmation email skipped.", message.AppointmentId);
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString) || string.IsNullOrWhiteSpace(options.Value.SenderAddress))
        {
            logger.LogWarning("Email configuration is missing. Confirmation email for appointment {AppointmentId} skipped.", message.AppointmentId);
            return;
        }

        var client = new EmailClient(options.Value.ConnectionString);
        var emailMessage = new EmailMessage(
            senderAddress: options.Value.SenderAddress,
            recipientAddress: message.CustomerEmail,
            content: new EmailContent("Potwierdzenie rezerwacji wizyty")
            {
                PlainText = BuildBody(message)
            });

        await client.SendAsync(WaitUntil.Started, emailMessage, cancellationToken);
        logger.LogInformation("Confirmation email queued for appointment {AppointmentId} to {CustomerEmail}.", message.AppointmentId, message.CustomerEmail);
    }

    private static string BuildBody(AppointmentBookedMessage message)
    {
        var customerName = string.IsNullOrWhiteSpace(message.CustomerName) ? "Kliencie" : message.CustomerName;
        var serviceName = string.IsNullOrWhiteSpace(message.ServiceName) ? message.SalonServiceId : message.ServiceName;
        var hairdresserName = string.IsNullOrWhiteSpace(message.HairdresserName) ? message.HairdresserId : message.HairdresserName;

        return $"""
        Dzien dobry {customerName},

        Twoja wizyta w salonie fryzjerskim zostala potwierdzona.

        Usluga: {serviceName}
        Fryzjer: {hairdresserName}
        Start: {message.StartAt:yyyy-MM-dd HH:mm}
        Koniec: {message.EndAt:yyyy-MM-dd HH:mm}

        Do zobaczenia!
        Salon fryzjerski
        """;
    }
}
