using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;

namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class SeedDataHostedService : IHostedService
    {
        private readonly IBookingRepository<Customer> _customers;
        private readonly IBookingRepository<Hairdresser> _hairdressers;
        private readonly IBookingRepository<SalonService> _services;

        public SeedDataHostedService(
            IBookingRepository<Customer> customers,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services)
        {
            _customers = customers;
            _hairdressers = hairdressers;
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if ((await _customers.GetAllAsync(cancellationToken)).Count == 0)
            {
                await _customers.CreateAsync(new Customer
                {
                    id = "customer-demo",
                    FirstName = "Anna",
                    LastName = "Kowalska",
                    PhoneNumber = "+48123123123",
                    Email = "anna@example.com"
                }, cancellationToken);
            }

            if ((await _hairdressers.GetAllAsync(cancellationToken)).Count == 0)
            {
                await _hairdressers.CreateAsync(new Hairdresser
                {
                    id = "hairdresser-demo",
                    FirstName = "Marek",
                    LastName = "Nowak",
                    Specialization = "Strzyżenie i koloryzacja"
                }, cancellationToken);
            }

            if ((await _services.GetAllAsync(cancellationToken)).Count == 0)
            {
                await _services.CreateAsync(new SalonService
                {
                    id = "service-demo",
                    Name = "Strzyżenie damskie",
                    Description = "Mycie, strzyżenie i modelowanie",
                    DurationMinutes = 60,
                    Price = 120
                }, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
