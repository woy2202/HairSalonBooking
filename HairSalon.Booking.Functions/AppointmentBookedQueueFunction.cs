using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HairSalon.Booking.Functions;

public sealed class AppointmentBookedQueueFunction(ILogger<AppointmentBookedQueueFunction> logger)
{
    [Function(nameof(AppointmentBookedQueueFunction))]
    public void Run([QueueTrigger("booking-notifications", Connection = "AzureWebJobsStorage")] string message)
    {
        var appointment = JsonSerializer.Deserialize<AppointmentBookedMessage>(message);
        logger.LogInformation(
            "Appointment {AppointmentId} should receive confirmation. Customer={CustomerId}, Hairdresser={HairdresserId}, Service={ServiceId}, Start={StartAt}",
            appointment?.AppointmentId,
            appointment?.CustomerId,
            appointment?.HairdresserId,
            appointment?.SalonServiceId,
            appointment?.StartAt);
    }
}

public sealed record AppointmentBookedMessage(
    string AppointmentId,
    string CustomerId,
    string HairdresserId,
    string SalonServiceId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt);
