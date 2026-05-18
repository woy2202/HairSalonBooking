using HairSalon.Booking.Api.Hubs;
using HairSalon.Booking.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class NotificationsController : ControllerBase
    {
        private readonly IHubContext<BookingNotificationsHub> _hubContext;
        private readonly ICurrentUserService _currentUser;

        public NotificationsController(
            IHubContext<BookingNotificationsHub> hubContext,
            ICurrentUserService currentUser)
        {
            _hubContext = hubContext;
            _currentUser = currentUser;
        }

        [HttpGet("signalr-test")]
        public async Task<IActionResult> SignalRTestPage(CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może uruchomić test SignalR." });
            }

            const string html = """
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8" />
              <title>Test SignalR</title>
              <style>
                body { font-family: Arial, sans-serif; margin: 32px; }
                pre { padding: 16px; background: #111827; color: #d1fae5; min-height: 220px; white-space: pre-wrap; }
                button { padding: 10px 14px; cursor: pointer; }
              </style>
            </head>
            <body>
              <h1>Test SignalR</h1>
              <button id="testButton">Wyślij testowe powiadomienie</button>
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
                  write("Powiadomienie testowe: " + JSON.stringify(message));
                });

                connection.on("appointmentBooked", message => {
                  write("Nowa wizyta: " + JSON.stringify(message));
                });

                connection.on("hairdresserAppointmentBooked", message => {
                  write("Nowa wizyta fryzjera: " + JSON.stringify(message));
                });

                connection.start()
                  .then(() => connection.invoke("JoinAdminGroup"))
                  .then(() => write("Połączono z SignalR jako administrator"))
                  .catch(err => write("Błąd połączenia: " + err));

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
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wysłać testowe powiadomienie SignalR." });
            }

            var payload = new
            {
                type = "test",
                message = "Testowe powiadomienie SignalR z HairSalon.Booking.Api",
                sentAt = DateTimeOffset.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("testNotification", payload, cancellationToken);
            return Ok(payload);
        }
    }
}
