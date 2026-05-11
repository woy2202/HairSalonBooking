using System.Text;
using Azure.Storage.Blobs;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Infrastructure;

public sealed class AzureBlobAppointmentSummaryHandler(IOptions<AzureBookingOptions> options) : IAppointmentBookedHandler
{
    public async Task HandleAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var connectionString = options.Value.Storage.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var container = new BlobContainerClient(connectionString, options.Value.Storage.AppointmentBlobContainer);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var summary = $"""
        Appointment: {appointment.id}
        Customer: {appointment.CustomerId}
        Hairdresser: {appointment.HairdresserId}
        Service: {appointment.SalonServiceId}
        Start: {appointment.StartAt:u}
        End: {appointment.EndAt:u}
        Status: {appointment.Status}
        """;

        var bytes = BinaryData.FromBytes(Encoding.UTF8.GetBytes(summary));
        await container.UploadBlobAsync($"{appointment.id}.txt", bytes, cancellationToken);
    }
}
