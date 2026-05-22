using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Functions.Email
{
    public sealed class AzureCommunicationEmailSender : IEmailSender
    {
        private readonly IOptions<EmailOptions> _options;
        private readonly ILogger<AzureCommunicationEmailSender> _logger;

        public AzureCommunicationEmailSender(
            IOptions<EmailOptions> options,
            ILogger<AzureCommunicationEmailSender> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task SendAppointmentConfirmationAsync(AppointmentBookedMessage message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(message.CustomerEmail))
            {
                _logger.LogWarning("Wizyta {AppointmentId} nie ma adresu e-mail klienta. Pominięto wysyłkę potwierdzenia.", message.AppointmentId);
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.Value.ConnectionString) || string.IsNullOrWhiteSpace(_options.Value.SenderAddress))
            {
                _logger.LogWarning("Brakuje konfiguracji e-mail. Pominięto wysyłkę potwierdzenia wizyty {AppointmentId}.", message.AppointmentId);
                return;
            }

            var client = new EmailClient(_options.Value.ConnectionString);
            var emailMessage = new EmailMessage(
                senderAddress: _options.Value.SenderAddress,
                recipientAddress: message.CustomerEmail,
                content: new EmailContent("Potwierdzenie rezerwacji wizyty")
                {
                    PlainText = BuildConfirmationBody(message)
                });

            await client.SendAsync(WaitUntil.Started, emailMessage, cancellationToken);
            _logger.LogInformation("Potwierdzenie e-mail dla wizyty {AppointmentId} zostało przekazane do wysłania na adres {CustomerEmail}.", message.AppointmentId, message.CustomerEmail);
        }

        public async Task<bool> SendAppointmentReminderAsync(AppointmentReminderMessage message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(message.CustomerEmail))
            {
                _logger.LogWarning("Wizyta {AppointmentId} nie ma adresu e-mail klienta. Pominięto wysyłkę przypomnienia.", message.AppointmentId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_options.Value.ConnectionString) || string.IsNullOrWhiteSpace(_options.Value.SenderAddress))
            {
                _logger.LogWarning("Brakuje konfiguracji e-mail. Pominięto wysyłkę przypomnienia wizyty {AppointmentId}.", message.AppointmentId);
                return false;
            }

            var client = new EmailClient(_options.Value.ConnectionString);
            var emailMessage = new EmailMessage(
                senderAddress: _options.Value.SenderAddress,
                recipientAddress: message.CustomerEmail,
                content: new EmailContent("Przypomnienie o jutrzejszej wizycie")
                {
                    PlainText = BuildReminderBody(message)
                });

            await client.SendAsync(WaitUntil.Started, emailMessage, cancellationToken);
            _logger.LogInformation("Przypomnienie e-mail dla wizyty {AppointmentId} zostało przekazane do wysłania na adres {CustomerEmail}.", message.AppointmentId, message.CustomerEmail);
            return true;
        }

        private static string BuildConfirmationBody(AppointmentBookedMessage message)
        {
            var customerName = string.IsNullOrWhiteSpace(message.CustomerName) ? "Kliencie" : message.CustomerName;
            var serviceName = string.IsNullOrWhiteSpace(message.ServiceName) ? message.SalonServiceId : message.ServiceName;
            var hairdresserName = string.IsNullOrWhiteSpace(message.HairdresserName) ? message.HairdresserId : message.HairdresserName;

            return $"""
            Dzień dobry {customerName},

            Twoja wizyta w salonie fryzjerskim została potwierdzona.

            Usługa: {serviceName}
            Fryzjer: {hairdresserName}
            Data wizyty: {message.StartAt:yyyy-MM-dd HH:mm}

            Do zobaczenia!
            Salon fryzjerski
            """;
        }

        private static string BuildReminderBody(AppointmentReminderMessage message)
        {
            var customerName = string.IsNullOrWhiteSpace(message.CustomerName) ? "Kliencie" : message.CustomerName;
            var serviceName = string.IsNullOrWhiteSpace(message.ServiceName) ? message.SalonServiceId : message.ServiceName;
            var hairdresserName = string.IsNullOrWhiteSpace(message.HairdresserName) ? message.HairdresserId : message.HairdresserName;

            return $"""
            Dzień dobry {customerName},

            Przypominamy o Twojej jutrzejszej wizycie w salonie fryzjerskim.

            Usługa: {serviceName}
            Fryzjer: {hairdresserName}
            Data wizyty: {message.StartAt:yyyy-MM-dd HH:mm}

            Do zobaczenia!
            Salon fryzjerski
            """;
        }
    }
}
