using System.Text.Json;
using HairSalon.Booking.Functions.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HairSalon.Booking.Functions;

public sealed class AppointmentBookedQueueFunction(
    ILogger<AppointmentBookedQueueFunction> logger,
    IEmailSender emailSender)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Function(nameof(AppointmentBookedQueueFunction))]
    public async Task Run([QueueTrigger("booking-notifications", Connection = "AzureWebJobsStorage")] string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Queue message received by AppointmentBookedQueueFunction.");

        var appointment = JsonSerializer.Deserialize<AppointmentBookedMessage>(message, JsonOptions);
        if (appointment is null)
        {
            logger.LogWarning("Queue message could not be deserialized: {Message}", message);
            return;
        }

        logger.LogInformation(
            "Appointment {AppointmentId} should receive confirmation. Customer={CustomerId}, Hairdresser={HairdresserId}, Service={ServiceId}, Start={StartAt}",
            appointment.AppointmentId,
            appointment.CustomerId,
            appointment.HairdresserId,
            appointment.SalonServiceId,
            appointment.StartAt);

        try
        {
            await emailSender.SendAppointmentConfirmationAsync(appointment, cancellationToken);
            logger.LogInformation("Appointment confirmation processing finished for {AppointmentId}.", appointment.AppointmentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Appointment confirmation processing failed for {AppointmentId}.", appointment.AppointmentId);
            throw;
        }
    }
}

public sealed record AppointmentBookedMessage(
    string AppointmentId,
    string CustomerId,
    string HairdresserId,
    string SalonServiceId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? CustomerEmail,
    string? CustomerName,
    string? HairdresserName,
    string? ServiceName);
