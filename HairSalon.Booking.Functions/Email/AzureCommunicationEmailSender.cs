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
                _logger.LogWarning("Wizyta {AppointmentId} nie ma adresu e-mail klienta. Pominiêto wysy³kê potwierdzenia.", message.AppointmentId);
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.Value.ConnectionString) || string.IsNullOrWhiteSpace(_options.Value.SenderAddress))
            {
                _logger.LogWarning("Brakuje konfiguracji e-mail. Pominiêto wysy³kê potwierdzenia wizyty {AppointmentId}.", message.AppointmentId);
                return;
            }

            var client = new EmailClient(_options.Value.ConnectionString);
            var emailMessage = new EmailMessage(
                senderAddress: _options.Value.SenderAddress,
                recipientAddress: message.CustomerEmail,
                content: new EmailContent("Potwierdzenie rezerwacji wizyty")
                {
                    PlainText = BuildBody(message)
                });

            await client.SendAsync(WaitUntil.Started, emailMessage, cancellationToken);
            _logger.LogInformation("Potwierdzenie e-mail dla wizyty {AppointmentId} zosta³o przekazane do wys³ania na adres {CustomerEmail}.", message.AppointmentId, message.CustomerEmail);
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
            Data wizyty: {message.StartAt:yyyy-MM-dd HH:mm}
            

            Do zobaczenia!
            Salon fryzjerski
            """;
        }
    }
}
