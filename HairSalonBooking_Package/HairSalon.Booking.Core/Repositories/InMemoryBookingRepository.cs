using System.Collections.Concurrent;
using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Repositories;

public sealed class InMemoryBookingRepository<T> : IBookingRepository<T> where T : BookingEntity
{
    private readonly ConcurrentDictionary<string, T> _items = new();

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<T>>(_items.Values.OrderBy(x => x.id).ToList());
    }

    public Task<T?> GetAsync(string id, CancellationToken cancellationToken)
    {
        _items.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<T> CreateAsync(T entity, CancellationToken cancellationToken)
    {
        entity.id = string.IsNullOrWhiteSpace(entity.id) ? Guid.NewGuid().ToString("N") : entity.id;
        _items[entity.id] = entity;
        return Task.FromResult(entity);
    }

    public Task<T> UpsertAsync(T entity, CancellationToken cancellationToken)
    {
        _items[entity.id] = entity;
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        _items.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
