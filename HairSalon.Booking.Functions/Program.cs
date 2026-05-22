using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using HairSalon.Booking.Functions.Email;
using HairSalon.Booking.Functions.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HairSalon.Booking.Functions
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(ConfigureServices)
                .Build();

            host.Run();
        }

        private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder configuration)
        {
            var builtConfiguration = configuration.Build();
            var keyVaultUri = builtConfiguration["Azure:KeyVault:VaultUri"];
            if (string.IsNullOrWhiteSpace(keyVaultUri))
            {
                keyVaultUri = builtConfiguration["KeyVault:VaultUri"];
            }

            if (!string.IsNullOrWhiteSpace(keyVaultUri))
            {
                configuration.AddAzureKeyVault(
                    new Uri(keyVaultUri),
                    new DefaultAzureCredential(),
                    new AzureKeyVaultConfigurationOptions());
            }
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
            services.Configure<EmailOptions>(context.Configuration.GetSection("Email"));
            services.Configure<CosmosOptions>(context.Configuration.GetSection("Azure:Cosmos"));
            services.AddSingleton<IEmailSender, AzureCommunicationEmailSender>();
        }
    }
}
