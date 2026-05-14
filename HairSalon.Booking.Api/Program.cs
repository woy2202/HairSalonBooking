using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Hubs;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["Azure:KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential(), new AzureKeyVaultConfigurationOptions());
}

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRTestCors", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Hair Salon Booking API",
        Version = "v1",
        Description = "Backend salonu fryzjerskiego: klienci, fryzjerzy, wizyty, uslugi, Cosmos DB, Blob, Queue i Azure Functions."
    });
});
builder.Services.AddBookingApplication(builder.Configuration);

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseCors("SignalRTestCors");

app.UseAuthorization();

app.MapControllers();
app.MapHub<BookingNotificationsHub>("/hubs/booking-notifications");

app.Run();
