using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using HairSalon.Booking.Functions.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, configuration) =>
    {
        var builtConfiguration = configuration.Build();
        var keyVaultUri = builtConfiguration["KeyVault:VaultUri"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential(), new AzureKeyVaultConfigurationOptions());
        }
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.Configure<EmailOptions>(context.Configuration.GetSection("Email"));
        services.AddSingleton<IEmailSender, AzureCommunicationEmailSender>();
    })
    .Build();

host.Run();
