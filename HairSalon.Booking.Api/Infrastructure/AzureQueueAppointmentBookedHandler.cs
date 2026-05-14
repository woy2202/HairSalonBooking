using System.Text.Json;
using Azure.Storage.Queues;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Infrastructure;

public sealed class AzureQueueAppointmentBookedHandler(
    IOptions<AzureBookingOptions> options,
    ILogger<AzureQueueAppointmentBookedHandler> logger,
    IBookingRepository<Customer> customers,
    IBookingRepository<Hairdresser> hairdressers,
    IBookingRepository<SalonService> services) : IAppointmentBookedHandler
{
    public async Task HandleAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var connectionString = options.Value.Storage.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Azure Storage connection string is missing. Queue message for appointment {AppointmentId} was not sent.", appointment.id);
            return;
        }

        var queue = new QueueClient(
            connectionString,
            options.Value.Storage.AppointmentQueueName,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });

        await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var customer = await customers.GetAsync(appointment.CustomerId, cancellationToken);
        var hairdresser = await hairdressers.GetAsync(appointment.HairdresserId, cancellationToken);
        var service = await services.GetAsync(appointment.SalonServiceId, cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            appointmentId = appointment.id,
            appointment.CustomerId,
            appointment.HairdresserId,
            appointment.SalonServiceId,
            appointment.StartAt,
            appointment.EndAt,
            customerEmail = customer?.Email,
            customerName = customer is null ? null : $"{customer.FirstName} {customer.LastName}".Trim(),
            hairdresserName = hairdresser is null ? null : $"{hairdresser.FirstName} {hairdresser.LastName}".Trim(),
            serviceName = service?.Name
        });

        await queue.SendMessageAsync(payload, cancellationToken);
        logger.LogInformation(
            "Queue message sent for appointment {AppointmentId} to queue {QueueName}. CustomerEmail={CustomerEmail}",
            appointment.id,
            options.Value.Storage.AppointmentQueueName,
            customer?.Email);
    }
}
