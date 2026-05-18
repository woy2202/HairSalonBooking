using System.Text.Json;
using HairSalon.Booking.Functions.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HairSalon.Booking.Functions
{
    public sealed class AppointmentBookedQueueFunction
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ILogger<AppointmentBookedQueueFunction> _logger;
        private readonly IEmailSender _emailSender;

        public AppointmentBookedQueueFunction(
            ILogger<AppointmentBookedQueueFunction> logger,
            IEmailSender emailSender)
        {
            _logger = logger;
            _emailSender = emailSender;
        }

        [Function(nameof(AppointmentBookedQueueFunction))]
        public async Task Run([QueueTrigger("%AppointmentQueueName%", Connection = "AzureWebJobsStorage")] string message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Odebrano wiadomoœæ z kolejki przez AppointmentBookedQueueFunction.");

            var appointment = JsonSerializer.Deserialize<AppointmentBookedMessage>(message, JsonOptions);
            if (appointment is null)
            {
                _logger.LogWarning("Nie uda³o siê odczytaæ wiadomoœci z kolejki: {Message}", message);
                return;
            }

            _logger.LogInformation(
                "Wizyta {AppointmentId} zosta³a przekazana do wys³ania potwierdzenia. Klient={CustomerId}, Fryzjer={HairdresserId}, Us³uga={ServiceId}, Data wizyty={StartAt}",
                appointment.AppointmentId,
                appointment.CustomerId,
                appointment.HairdresserId,
                appointment.SalonServiceId,
                appointment.StartAt);

            try
            {
                await _emailSender.SendAppointmentConfirmationAsync(appointment, cancellationToken);
                _logger.LogInformation("Zakoñczono obs³ugê potwierdzenia wizyty {AppointmentId}.", appointment.AppointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie uda³o siê obs³u¿yæ potwierdzenia wizyty {AppointmentId}.", appointment.AppointmentId);
                throw;
            }
        }
    }

    public sealed class AppointmentBookedMessage
    {
        public string AppointmentId { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        public string HairdresserId { get; set; } = string.Empty;

        public string SalonServiceId { get; set; } = string.Empty;

        public DateTimeOffset StartAt { get; set; }

        public DateTimeOffset EndAt { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CustomerName { get; set; }

        public string? HairdresserName { get; set; }

        public string? ServiceName { get; set; }
    }
}
