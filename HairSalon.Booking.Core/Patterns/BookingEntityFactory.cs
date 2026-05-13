using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Patterns;

// Factory Method: centralizes creation of domain entities with valid partition keys.
public interface IBookingEntityFactory
{
    BookingEntity Create(EntityKind kind);
}

public sealed class BookingEntityFactory : IBookingEntityFactory
{
    public BookingEntity Create(EntityKind kind) => kind switch
    {
        EntityKind.Customer => new Customer(),
        EntityKind.Hairdresser => new Hairdresser(),
        EntityKind.SalonService => new SalonService(),
        EntityKind.Appointment => new Appointment(),
        EntityKind.SalonPhoto => new SalonPhoto(),
        EntityKind.AppUser => new AppUser(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported booking entity kind.")
    };
}
