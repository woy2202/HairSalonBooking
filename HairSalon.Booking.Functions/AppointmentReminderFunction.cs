using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Functions.Email;
using HairSalon.Booking.Functions.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Functions
{
    public sealed class AppointmentReminderFunction
    {
        private const string AppointmentPartitionKey = nameof(Appointment);
        private const string CustomerPartitionKey = nameof(Customer);
        private const string HairdresserPartitionKey = nameof(Hairdresser);
        private const string SalonServicePartitionKey = nameof(SalonService);

        private readonly IOptions<CosmosOptions> _options;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AppointmentReminderFunction> _logger;

        public AppointmentReminderFunction(
            IOptions<CosmosOptions> options,
            IEmailSender emailSender,
            ILogger<AppointmentReminderFunction> logger)
        {
            _options = options;
            _emailSender = emailSender;
            _logger = logger;
        }

        [Function(nameof(AppointmentReminderFunction))]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timer, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.Value.ConnectionString))
            {
                _logger.LogWarning("Brakuje konfiguracji Cosmos DB. Pominięto wysyłkę przypomnień o wizytach.");
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var reminderWindowStart = now.AddHours(23);
            var reminderWindowEnd = now.AddHours(25);

            using var client = new CosmosClient(_options.Value.ConnectionString);
            var database = client.GetDatabase(_options.Value.DatabaseName);
            var appointmentsContainer = database.GetContainer(_options.Value.AppointmentsContainerName);
            var customersContainer = database.GetContainer(_options.Value.CustomersContainerName);
            var hairdressersContainer = database.GetContainer(_options.Value.HairdressersContainerName);
            var servicesContainer = database.GetContainer(_options.Value.SalonServicesContainerName);

            var appointments = await GetAllAsync<Appointment>(
                appointmentsContainer,
                AppointmentPartitionKey,
                cancellationToken);

            var appointmentsForReminder = appointments
                .Where(appointment => ShouldSendReminder(appointment, reminderWindowStart, reminderWindowEnd))
                .OrderBy(appointment => appointment.StartAt)
                .ToList();

            _logger.LogInformation(
                "Znaleziono {Count} wizyt wymagających przypomnienia w oknie od {From} do {To}.",
                appointmentsForReminder.Count,
                reminderWindowStart,
                reminderWindowEnd);

            foreach (var appointment in appointmentsForReminder)
            {
                await SendReminderAsync(
                    appointment,
                    appointmentsContainer,
                    customersContainer,
                    hairdressersContainer,
                    servicesContainer,
                    now,
                    cancellationToken);
            }
        }

        private async Task SendReminderAsync(
            Appointment appointment,
            Container appointmentsContainer,
            Container customersContainer,
            Container hairdressersContainer,
            Container servicesContainer,
            DateTimeOffset sentAt,
            CancellationToken cancellationToken)
        {
            var customer = await GetOptionalAsync<Customer>(customersContainer, appointment.CustomerId, cancellationToken);
            var hairdresser = await GetOptionalAsync<Hairdresser>(hairdressersContainer, appointment.HairdresserId, cancellationToken);
            var service = await GetOptionalAsync<SalonService>(servicesContainer, appointment.SalonServiceId, cancellationToken);

            var message = new AppointmentReminderMessage
            {
                AppointmentId = appointment.id,
                CustomerId = appointment.CustomerId,
                HairdresserId = appointment.HairdresserId,
                SalonServiceId = appointment.SalonServiceId,
                StartAt = appointment.StartAt,
                EndAt = appointment.EndAt,
                CustomerEmail = customer?.Email,
                CustomerName = customer is null ? null : $"{customer.FirstName} {customer.LastName}".Trim(),
                HairdresserName = hairdresser is null ? null : $"{hairdresser.FirstName} {hairdresser.LastName}".Trim(),
                ServiceName = service?.Name
            };

            var sent = await _emailSender.SendAppointmentReminderAsync(message, cancellationToken);
            if (!sent)
            {
                return;
            }

            appointment.ReminderOneDayBeforeSentAt = sentAt;
            appointment.partitionKey = AppointmentPartitionKey;
            await appointmentsContainer.UpsertItemAsync(appointment, new PartitionKey(appointment.id), cancellationToken: cancellationToken);
            _logger.LogInformation("Oznaczono przypomnienie dla wizyty {AppointmentId} jako wysłane.", appointment.id);
        }

        private static bool ShouldSendReminder(
            Appointment appointment,
            DateTimeOffset reminderWindowStart,
            DateTimeOffset reminderWindowEnd)
        {
            if (appointment.ReminderOneDayBeforeSentAt is not null)
            {
                return false;
            }

            if (appointment.Status != AppointmentStatus.Booked &&
                appointment.Status != AppointmentStatus.Confirmed)
            {
                return false;
            }

            var startAt = appointment.StartAt.ToUniversalTime();
            return startAt >= reminderWindowStart && startAt <= reminderWindowEnd;
        }

        private static async Task<IReadOnlyList<T>> GetAllAsync<T>(
            Container container,
            string partitionKey,
            CancellationToken cancellationToken)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.partitionKey = @partitionKey")
                .WithParameter("@partitionKey", partitionKey);
            using var iterator = container.GetItemQueryIterator<T>(query);
            var results = new List<T>();

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(page);
            }

            return results;
        }

        private static async Task<T?> GetOptionalAsync<T>(
            Container container,
            string id,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return default;
            }

            try
            {
                var response = await container.ReadItemAsync<T>(
                    id,
                    new PartitionKey(id),
                    cancellationToken: cancellationToken);

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default;
            }
        }
    }

    public sealed class AppointmentReminderMessage
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
