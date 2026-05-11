using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.Azure.Cosmos;

namespace HairSalon.Booking.Api.Data;

public sealed class CosmosBookingRepository<T>(Container container) : IBookingRepository<T> where T : BookingEntity, new()
{
    private static readonly string EntityPartitionKey = new T().partitionKey;

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.partitionKey = @partitionKey")
            .WithParameter("@partitionKey", EntityPartitionKey);
        using var iterator = container.GetItemQueryIterator<T>(query);
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
            return await container.ReadItemAsync<T>(id, new PartitionKey(EntityPartitionKey), cancellationToken: cancellationToken);
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
        var response = await container.CreateItemAsync(entity, new PartitionKey(entity.partitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<T> UpsertAsync(T entity, CancellationToken cancellationToken)
    {
        entity.partitionKey = EntityPartitionKey;
        var response = await container.UpsertItemAsync(entity, new PartitionKey(entity.partitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await container.DeleteItemAsync<T>(id, new PartitionKey(EntityPartitionKey), cancellationToken: cancellationToken);
    }
}
