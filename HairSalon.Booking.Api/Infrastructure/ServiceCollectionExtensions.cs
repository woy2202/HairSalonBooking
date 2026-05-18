using HairSalon.Booking.Api.Data;
using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Patterns;
using HairSalon.Booking.Core.Repositories;
using HairSalon.Booking.Core.Services;
using Microsoft.Azure.Cosmos;

namespace HairSalon.Booking.Api.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBookingApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AzureBookingOptions>(configuration.GetSection("Azure"));
            services.AddHttpContextAccessor();
            services.AddSingleton<IBookingEntityFactory, BookingEntityFactory>();
            services.AddScoped<ICurrentUserService, EasyAuthCurrentUserService>();
            services.AddScoped<IPhotoStorageService, BlobPhotoStorageService>();
            services.AddScoped<IAppointmentBookingFacade, AppointmentBookingFacade>();
            services.AddScoped<IAppointmentBookedHandler, AzureQueueAppointmentBookedHandler>();
            services.AddScoped<IAppointmentBookedHandler, SignalRAppointmentBookedHandler>();
            services.AddScoped<IBookingEventPublisher, BookingEventPublisher>();

            var options = configuration.GetSection("Azure").Get<AzureBookingOptions>() ?? new AzureBookingOptions();
            var signalRBuilder = services.AddSignalR();
            if (!string.IsNullOrWhiteSpace(options.SignalR.ConnectionString))
            {
                signalRBuilder.AddAzureSignalR(options.SignalR.ConnectionString);
            }

            if (string.IsNullOrWhiteSpace(options.Cosmos.ConnectionString))
            {
                services.AddSingleton<IBookingRepository<Customer>, InMemoryBookingRepository<Customer>>();
                services.AddSingleton<IBookingRepository<Hairdresser>, InMemoryBookingRepository<Hairdresser>>();
                services.AddSingleton<IBookingRepository<SalonService>, InMemoryBookingRepository<SalonService>>();
                services.AddSingleton<IBookingRepository<Appointment>, InMemoryBookingRepository<Appointment>>();
                services.AddSingleton<IBookingRepository<SalonPhoto>, InMemoryBookingRepository<SalonPhoto>>();
                services.AddSingleton<IBookingRepository<AppUser>, InMemoryBookingRepository<AppUser>>();
                services.AddHostedService<SeedDataHostedService>();
                return services;
            }

            services.AddSingleton(_ => new CosmosClient(options.Cosmos.ConnectionString));
            services.AddSingleton<ICosmosContainerResolver, CosmosContainerResolver>();
            services.AddScoped(typeof(IBookingRepository<>), typeof(CosmosBookingRepository<>));
            return services;
        }
    }
}
