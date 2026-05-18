using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Patterns
{
    public interface IBookingEntityFactory
    {
        BookingEntity Create(EntityKind kind);
    }

    public sealed class BookingEntityFactory : IBookingEntityFactory
    {
        public BookingEntity Create(EntityKind kind)
        {
            switch (kind)
            {
                case EntityKind.Customer:
                    return new Customer();
                case EntityKind.Hairdresser:
                    return new Hairdresser();
                case EntityKind.SalonService:
                    return new SalonService();
                case EntityKind.Appointment:
                    return new Appointment();
                case EntityKind.SalonPhoto:
                    return new SalonPhoto();
                case EntityKind.AppUser:
                    return new AppUser();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Nieobsługiwany typ encji rezerwacji.");
            }
        }
    }
}
