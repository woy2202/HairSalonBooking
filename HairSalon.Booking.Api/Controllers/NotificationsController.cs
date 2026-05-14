using HairSalon.Booking.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HairSalon.Booking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController(IHubContext<BookingNotificationsHub> hubContext) : ControllerBase
{
    [HttpGet("signalr-test")]
    public ContentResult SignalRTestPage()
    {
        const string html = """
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8" />
          <title>SignalR Test</title>
          <style>
            body { font-family: Arial, sans-serif; margin: 32px; }
            pre { padding: 16px; background: #111827; color: #d1fae5; min-height: 220px; white-space: pre-wrap; }
            button { padding: 10px 14px; cursor: pointer; }
          </style>
        </head>
        <body>
          <h1>SignalR Test</h1>
          <button id="testButton">Send test notification</button>
          <pre id="log"></pre>

          <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.7/dist/browser/signalr.min.js"></script>
          <script>
            const log = document.getElementById("log");
            const button = document.getElementById("testButton");

            function write(message) {
              log.textContent += message + "\n";
            }

            const connection = new signalR.HubConnectionBuilder()
              .withUrl("/hubs/booking-notifications")
              .withAutomaticReconnect()
              .build();

            connection.on("testNotification", message => {
              write("testNotification: " + JSON.stringify(message));
            });

            connection.on("appointmentBooked", message => {
              write("appointmentBooked: " + JSON.stringify(message));
            });

            connection.on("hairdresserAppointmentBooked", message => {
              write("hairdresserAppointmentBooked: " + JSON.stringify(message));
            });

            connection.start()
              .then(() => write("Connected to SignalR"))
              .catch(err => write("Connection error: " + err));

            button.addEventListener("click", async () => {
              const response = await fetch("/api/Notifications/test", { method: "POST" });
              write("POST /api/Notifications/test -> " + response.status);
            });
          </script>
        </body>
        </html>
        """;

        return Content(html, "text/html");
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification(CancellationToken cancellationToken)
    {
        var payload = new
        {
            type = "test",
            message = "SignalR notification from HairSalon.Booking.Api",
            sentAt = DateTimeOffset.UtcNow
        };

        await hubContext.Clients.All.SendAsync("testNotification", payload, cancellationToken);
        return Ok(payload);
    }
}
