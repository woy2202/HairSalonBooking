using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using HairSalon.Booking.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class HairdressersController : ControllerBase
    {
        private readonly IBookingRepository<Hairdresser> _repository;
        private readonly IBookingRepository<Appointment> _appointments;
        private readonly IBookingRepository<Customer> _customers;
        private readonly IBookingRepository<SalonService> _services;
        private readonly IAppointmentBookingFacade _bookingFacade;
        private readonly IPhotoStorageService _photoStorage;
        private readonly ICurrentUserService _currentUser;
        private readonly IOptions<AzureBookingOptions> _options;

        public HairdressersController(
            IBookingRepository<Hairdresser> repository,
            IBookingRepository<Appointment> appointments,
            IBookingRepository<Customer> customers,
            IBookingRepository<SalonService> services,
            IAppointmentBookingFacade bookingFacade,
            IPhotoStorageService photoStorage,
            ICurrentUserService currentUser,
            IOptions<AzureBookingOptions> options)
        {
            _repository = repository;
            _appointments = appointments;
            _customers = customers;
            _services = services;
            _bookingFacade = bookingFacade;
            _photoStorage = photoStorage;
            _currentUser = currentUser;
            _options = options;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Hairdresser>>> GetAll(CancellationToken cancellationToken)
        {
            var hairdressers = await _repository.GetAllAsync(cancellationToken);
            return Ok(hairdressers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Hairdresser>> GetById(string id, CancellationToken cancellationToken)
        {
            var hairdresser = await _repository.GetAsync(id, cancellationToken);
            return hairdresser is null ? NotFound() : Ok(hairdresser);
        }

        [HttpGet("me/appointments")]
        public async Task<ActionResult<IReadOnlyList<Appointment>>> GetMyAppointments(CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.HairdresserId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany u¿ytkownik nie ma przypisanego profilu fryzjera." });
            }

            var allAppointments = await _appointments.GetAllAsync(cancellationToken);
            var hairdresserAppointments = allAppointments
                .Where(appointment => appointment.HairdresserId == user.HairdresserId)
                .OrderBy(appointment => appointment.StartAt)
                .ToList();

            return Ok(hairdresserAppointments);
        }

        [HttpGet("me/customers/{customerId}/history")]
        public async Task<ActionResult> GetCustomerHistory(string customerId, CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.HairdresserId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany u¿ytkownik nie ma przypisanego profilu fryzjera." });
            }

            var customer = await _customers.GetAsync(customerId, cancellationToken);
            if (customer is null)
            {
                return NotFound(new { error = "Nie znaleziono klienta." });
            }

            var allAppointments = await _appointments.GetAllAsync(cancellationToken);
            var customerAppointments = allAppointments
                .Where(appointment => appointment.CustomerId == customerId)
                .OrderByDescending(appointment => appointment.StartAt)
                .ToList();

            var hairdressers = await _repository.GetAllAsync(cancellationToken);
            var services = await _services.GetAllAsync(cancellationToken);

            var history = customerAppointments
                .Select(appointment => new
                {
                    Appointment = appointment,
                    Hairdresser = hairdressers.FirstOrDefault(hairdresser => hairdresser.id == appointment.HairdresserId),
                    Service = services.FirstOrDefault(service => service.id == appointment.SalonServiceId)
                })
                .ToList();

            var previousHistory = history
                .Where(item => item.Appointment.StartAt < DateTimeOffset.UtcNow)
                .ToList();

            var upcomingHistory = history
                .Where(item => item.Appointment.StartAt >= DateTimeOffset.UtcNow)
                .ToList();

            var usedServices = services
                .Where(service => customerAppointments.Any(appointment => appointment.SalonServiceId == service.id))
                .ToList();

            var visitedHairdressers = hairdressers
                .Where(hairdresser => customerAppointments.Any(appointment => appointment.HairdresserId == hairdresser.id))
                .ToList();

            return Ok(new
            {
                Customer = customer,
                History = history,
                PreviousHistory = previousHistory,
                UpcomingHistory = upcomingHistory,
                UsedServices = usedServices,
                VisitedHairdressers = visitedHairdressers
            });
        }

        [HttpGet("me/customers/{customerId}/my-history")]
        public async Task<ActionResult> GetMyCustomerHistory(string customerId, CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.HairdresserId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany u¿ytkownik nie ma przypisanego profilu fryzjera." });
            }

            var customer = await _customers.GetAsync(customerId, cancellationToken);
            if (customer is null)
            {
                return NotFound(new { error = "Nie znaleziono klienta." });
            }

            var allAppointments = await _appointments.GetAllAsync(cancellationToken);
            var hairdresserAppointmentsWithCustomer = allAppointments
                .Where(appointment => appointment.CustomerId == customerId && appointment.HairdresserId == user.HairdresserId)
                .OrderByDescending(appointment => appointment.StartAt)
                .ToList();

            if (hairdresserAppointmentsWithCustomer.Count == 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Ten fryzjer nie ma wspólnych wizyt z tym klientem, dlatego nie mo¿e odczytaæ jego historii." });
            }

            var services = await _services.GetAllAsync(cancellationToken);
            var usedServices = services
                .Where(service => hairdresserAppointmentsWithCustomer.Any(appointment => appointment.SalonServiceId == service.id))
                .ToList();

            return Ok(new
            {
                Customer = customer,
                PreviousAppointments = hairdresserAppointmentsWithCustomer.Where(appointment => appointment.StartAt < DateTimeOffset.UtcNow),
                UpcomingAppointments = hairdresserAppointmentsWithCustomer.Where(appointment => appointment.StartAt >= DateTimeOffset.UtcNow),
                UsedServices = usedServices
            });
        }

        [HttpGet("{id}/availability/{day}")]
        public async Task<ActionResult<IReadOnlyList<DateTimeOffset>>> GetAvailability(string id, DateOnly day, CancellationToken cancellationToken)
        {
            var slots = await _bookingFacade.GetAvailableSlotsAsync(id, day, cancellationToken);
            return Ok(slots);
        }

        [HttpPost("{id}/photo")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Hairdresser>> UploadPhoto(string id, IFormFile file, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator mo¿e dodawaæ zdjêcie fryzjera." });
            }

            var hairdresser = await _repository.GetAsync(id, cancellationToken);
            if (hairdresser is null)
            {
                return NotFound(new { error = "Nie znaleziono fryzjera." });
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(hairdresser.PhotoBlobName))
                {
                    await _photoStorage.DeleteAsync(_options.Value.Storage.HairdresserPhotoBlobContainer, hairdresser.PhotoBlobName, cancellationToken);
                }

                var uploaded = await _photoStorage.UploadAsync(_options.Value.Storage.HairdresserPhotoBlobContainer, file, cancellationToken);
                hairdresser.PhotoUrl = uploaded.Url;
                hairdresser.PhotoBlobName = uploaded.BlobName;
                hairdresser.PhotoDisplayWidth = uploaded.DisplayWidth;
                hairdresser.PhotoDisplayHeight = uploaded.DisplayHeight;

                return Ok(await _repository.UpsertAsync(hairdresser, cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Hairdresser>> Create(HairdresserRequest request, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator mo¿e dodawaæ fryzjerów." });
            }

            var hairdresser = Apply(new Hairdresser(), request);
            var created = await _repository.CreateAsync(hairdresser, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Hairdresser>> Update(string id, HairdresserRequest request, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator mo¿e edytowaæ fryzjerów." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono fryzjera do edycji." });
            }

            var updated = await _repository.UpsertAsync(Apply(existing, request), cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator mo¿e usuwaæ fryzjerów." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono fryzjera do usuniêcia." });
            }

            if (await HasAppointmentsAsync(id, cancellationToken))
            {
                return BadRequest(new { error = "Nie mo¿na usun¹æ fryzjera, poniewa¿ ma przypisane wizyty. Najpierw anuluj albo usuñ powi¹zane wizyty." });
            }

            await _repository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        private static Hairdresser Apply(Hairdresser hairdresser, HairdresserRequest request)
        {
            hairdresser.FirstName = request.FirstName;
            hairdresser.LastName = request.LastName;
            hairdresser.Specialization = request.Specialization;
            hairdresser.IsActive = request.IsActive;
            return hairdresser;
        }

        private async Task<bool> HasAppointmentsAsync(string hairdresserId, CancellationToken cancellationToken)
        {
            var appointments = await _appointments.GetAllAsync(cancellationToken);
            return appointments.Any(appointment => appointment.HairdresserId == hairdresserId);
        }
    }
}
