using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using HairSalon.Booking.Api.Hubs;
using HairSalon.Booking.Api.Infrastructure;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

namespace HairSalon.Booking.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            AddKeyVaultConfiguration(builder);
            ConfigureServices(builder);

            var app = builder.Build();
            ConfigureApplication(app);

            app.Run();
        }

        private static void AddKeyVaultConfiguration(WebApplicationBuilder builder)
        {
            var keyVaultUri = builder.Configuration["Azure:KeyVault:VaultUri"];
            if (!string.IsNullOrWhiteSpace(keyVaultUri))
            {
                builder.Configuration.AddAzureKeyVault(
                    new Uri(keyVaultUri),
                    new DefaultAzureCredential(),
                    new AzureKeyVaultConfigurationOptions());
            }
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Services.AddApplicationInsightsTelemetry();

            builder.Services.AddCors(options =>
            {
                var allowedOrigins = builder.Configuration
                    .GetSection("Azure:Cors:AllowedOrigins")
                    .Get<string[]>() ?? Array.Empty<string>();

                options.AddPolicy("FrontendCors", policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod();

                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins).AllowCredentials();
                    }
                });
            });

            builder.Services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "API rezerwacji wizyt w salonie fryzjerskim",
                    Version = "v1",
                    Description = "Backend salonu fryzjerskiego: klienci, fryzjerzy, wizyty, uslugi, Cosmos DB, Blob Storage, kolejki i Azure Functions."
                });
            });

            builder.Services.AddBookingApplication(builder.Configuration);
        }

        private static void ConfigureApplication(WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseCors("FrontendCors");
            app.UseMiddleware<EasyAuthGuardMiddleware>();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<BookingNotificationsHub>("/hubs/booking-notifications");
        }
    }
}
