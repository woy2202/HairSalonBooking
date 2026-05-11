using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;

namespace HairSalon.Booking.Api.Infrastructure;

public sealed class SeedDataHostedService(
    IBookingRepository<Customer> customers,
    IBookingRepository<Hairdresser> hairdressers,
    IBookingRepository<SalonService> services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if ((await customers.GetAllAsync(cancellationToken)).Count == 0)
        {
            await customers.CreateAsync(new Customer
            {
                id = "customer-demo",
                FirstName = "Anna",
                LastName = "Kowalska",
                PhoneNumber = "+48123123123",
                Email = "anna@example.com"
            }, cancellationToken);
        }

        if ((await hairdressers.GetAllAsync(cancellationToken)).Count == 0)
        {
            await hairdressers.CreateAsync(new Hairdresser
            {
                id = "hairdresser-demo",
                FirstName = "Marek",
                LastName = "Nowak",
                Specialization = "Strzyzenie i koloryzacja"
            }, cancellationToken);
        }

        if ((await services.GetAllAsync(cancellationToken)).Count == 0)
        {
            await services.CreateAsync(new SalonService
            {
                id = "service-demo",
                Name = "Strzyzenie damskie",
                Description = "Mycie, strzyzenie i modelowanie",
                DurationMinutes = 60,
                Price = 120
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
