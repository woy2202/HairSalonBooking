using System.Text.Json;
using Azure.Storage.Queues;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Infrastructure;

public sealed class AzureQueueAppointmentBookedHandler(IOptions<AzureBookingOptions> options) : IAppointmentBookedHandler
{
    public async Task HandleAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var connectionString = options.Value.Storage.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var queue = new QueueClient(connectionString, options.Value.Storage.AppointmentQueueName);
        await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            appointmentId = appointment.id,
            appointment.CustomerId,
            appointment.HairdresserId,
            appointment.SalonServiceId,
            appointment.StartAt,
            appointment.EndAt
        });

        await queue.SendMessageAsync(payload, cancellationToken);
    }
}
