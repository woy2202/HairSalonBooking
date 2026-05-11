using HairSalon.Booking.Api.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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

app.UseAuthorization();

app.MapControllers();

app.Run();
