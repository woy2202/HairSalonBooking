using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Data;

public sealed class CosmosContainerResolver : ICosmosContainerResolver
{
    private readonly Database _database;
    private readonly CosmosOptions _options;
    private readonly Dictionary<Type, Container> _containers = [];

    public CosmosContainerResolver(CosmosClient client, IOptions<AzureBookingOptions> options)
    {
        _options = options.Value.Cosmos;
        _database = client.CreateDatabaseIfNotExistsAsync(_options.DatabaseName).GetAwaiter().GetResult().Database;
    }

    public Container GetContainer<T>() where T : BookingEntity
    {
        var entityType = typeof(T);
        if (_containers.TryGetValue(entityType, out var existing))
        {
            return existing;
        }

        var containerName = GetContainerName(entityType);
        var container = _database
            .CreateContainerIfNotExistsAsync(containerName, _options.PartitionKeyPath)
            .GetAwaiter()
            .GetResult()
            .Container;

        _containers[entityType] = container;
        return container;
    }

    private string GetContainerName(Type entityType)
    {
        if (entityType == typeof(Customer))
        {
            return _options.CustomersContainerName;
        }

        if (entityType == typeof(Hairdresser))
        {
            return _options.HairdressersContainerName;
        }

        if (entityType == typeof(SalonService))
        {
            return _options.SalonServicesContainerName;
        }

        if (entityType == typeof(Appointment))
        {
            return _options.AppointmentsContainerName;
        }

        if (entityType == typeof(SalonPhoto))
        {
            return _options.SalonPhotosContainerName;
        }

        if (entityType == typeof(AppUser))
        {
            return _options.UsersContainerName;
        }

        throw new InvalidOperationException($"No Cosmos DB container configured for entity type {entityType.Name}.");
    }
}
