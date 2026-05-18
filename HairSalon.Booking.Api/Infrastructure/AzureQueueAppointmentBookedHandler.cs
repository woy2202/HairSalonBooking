using System.Text.Json;
using Azure.Storage.Queues;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class AzureQueueAppointmentBookedHandler : IAppointmentBookedHandler
    {
        private readonly IOptions<AzureBookingOptions> _options;
        private readonly ILogger<AzureQueueAppointmentBookedHandler> _logger;
        private readonly IBookingRepository<Customer> _customers;
        private readonly IBookingRepository<Hairdresser> _hairdressers;
        private readonly IBookingRepository<SalonService> _services;

        public AzureQueueAppointmentBookedHandler(
            IOptions<AzureBookingOptions> options,
            ILogger<AzureQueueAppointmentBookedHandler> logger,
            IBookingRepository<Customer> customers,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services)
        {
            _options = options;
            _logger = logger;
            _customers = customers;
            _hairdressers = hairdressers;
            _services = services;
        }

        public async Task HandleAsync(Appointment appointment, CancellationToken cancellationToken)
        {
            var connectionString = _options.Value.Storage.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("Brakuje parametru połączenia Azure Storage. Nie wysłano wiadomości kolejki dla wizyty {AppointmentId}.", appointment.id);
                return;
            }

            var queue = new QueueClient(
                connectionString,
                _options.Value.Storage.AppointmentQueueName,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });

            await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var customer = await _customers.GetAsync(appointment.CustomerId, cancellationToken);
            var hairdresser = await _hairdressers.GetAsync(appointment.HairdresserId, cancellationToken);
            var service = await _services.GetAsync(appointment.SalonServiceId, cancellationToken);

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
            _logger.LogInformation(
                "Wysłano wiadomość dla wizyty {AppointmentId} do kolejki {QueueName}. Email klienta={CustomerEmail}",
                appointment.id,
                _options.Value.Storage.AppointmentQueueName,
                customer?.Email);
        }
    }
}
