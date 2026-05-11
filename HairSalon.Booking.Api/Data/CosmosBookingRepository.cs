using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.Azure.Cosmos;

namespace HairSalon.Booking.Api.Data;

public sealed class CosmosBookingRepository<T>(ICosmosContainerResolver containerResolver) : IBookingRepository<T> where T : BookingEntity, new()
{
    private static readonly string EntityPartitionKey = new T().partitionKey;
    private readonly Container _container = containerResolver.GetContainer<T>();

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.partitionKey = @partitionKey")
            .WithParameter("@partitionKey", EntityPartitionKey);
        using var iterator = _container.GetItemQueryIterator<T>(query);
        var results = new List<T>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }

        return results;
    }

    public async Task<T?> GetAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            return await _container.ReadItemAsync<T>(id, new PartitionKey(id), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken)
    {
        entity.id = string.IsNullOrWhiteSpace(entity.id) ? Guid.NewGuid().ToString("N") : entity.id;
        entity.partitionKey = EntityPartitionKey;
        var response = await _container.CreateItemAsync(entity, new PartitionKey(entity.id), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<T> UpsertAsync(T entity, CancellationToken cancellationToken)
    {
        entity.partitionKey = EntityPartitionKey;
        var response = await _container.UpsertItemAsync(entity, new PartitionKey(entity.id), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _container.DeleteItemAsync<T>(id, new PartitionKey(id), cancellationToken: cancellationToken);
    }
}
