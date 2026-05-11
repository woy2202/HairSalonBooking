using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Repositories;

public interface IBookingRepository<T> where T : BookingEntity
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);
    Task<T?> GetAsync(string id, CancellationToken cancellationToken);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken);
    Task<T> UpsertAsync(T entity, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
