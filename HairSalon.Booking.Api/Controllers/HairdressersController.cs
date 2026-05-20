using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Model;
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
        private readonly IBookingRepository<AppUser> _users;
        private readonly IBookingRepository<Appointment> _appointments;
        private readonly IBookingRepository<Customer> _customers;
        private readonly IBookingRepository<SalonService> _services;
        private readonly IBookingRepository<Review> _reviews;
        private readonly IAppointmentBookingFacade _bookingFacade;
        private readonly IPhotoStorageService _photoStorage;
        private readonly ICurrentUserService _currentUser;
        private readonly IAppointmentStatusService _appointmentStatusService;
        private readonly IOptions<AzureBookingOptions> _options;

        public HairdressersController(
            IBookingRepository<Hairdresser> repository,
            IBookingRepository<AppUser> users,
            IBookingRepository<Appointment> appointments,
            IBookingRepository<Customer> customers,
            IBookingRepository<SalonService> services,
            IBookingRepository<Review> reviews,
            IAppointmentBookingFacade bookingFacade,
            IPhotoStorageService photoStorage,
            ICurrentUserService currentUser,
            IAppointmentStatusService appointmentStatusService,
            IOptions<AzureBookingOptions> options)
        {
            _repository = repository;
            _users = users;
            _appointments = appointments;
            _customers = customers;
            _services = services;
            _reviews = reviews;
            _bookingFacade = bookingFacade;
            _photoStorage = photoStorage;
            _currentUser = currentUser;
            _appointmentStatusService = appointmentStatusService;
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
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu fryzjera." });
            }

            await _appointmentStatusService.RefreshExpiredAppointmentsAsync(cancellationToken);
            var allAppointments = await _appointments.GetAllAsync(cancellationToken);
            var hairdresserAppointments = allAppointments
                .Where(appointment => appointment.HairdresserId == user.HairdresserId)
                .OrderBy(appointment => appointment.StartAt)
                .ToList();

            return Ok(hairdresserAppointments);
        }

        [HttpPatch("me/appointments/{appointmentId}/status")]
        public async Task<ActionResult<Appointment>> ChangeMyAppointmentStatus(string appointmentId, ChangeAppointmentStatusRequest request, CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.HairdresserId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu fryzjera." });
            }

            var appointment = await _appointments.GetAsync(appointmentId, cancellationToken);
            if (appointment is null)
            {
                return NotFound(new { error = "Nie znaleziono wizyty." });
            }

            appointment = await _appointmentStatusService.RefreshExpiredAppointmentAsync(appointment, cancellationToken);
            if (appointment.HairdresserId != user.HairdresserId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Fryzjer może zmieniać status tylko swoich wizyt." });
            }

            var validationError = ValidateHairdresserStatusChange(appointment, request.Status);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return BadRequest(new { error = validationError });
            }

            appointment.Status = request.Status;
            return Ok(await _appointments.UpsertAsync(appointment, cancellationToken));
        }

        [HttpGet("me/customers/{customerId}/history")]
        public async Task<ActionResult> GetCustomerHistory(string customerId, CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.HairdresserId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu fryzjera." });
            }

            var customer = await _customers.GetAsync(customerId, cancellationToken);
            if (customer is null)
            {
                return NotFound(new { error = "Nie znaleziono klienta." });
            }

            await _appointmentStatusService.RefreshExpiredAppointmentsAsync(cancellationToken);
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
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu fryzjera." });
            }

            var customer = await _customers.GetAsync(customerId, cancellationToken);
            if (customer is null)
            {
                return NotFound(new { error = "Nie znaleziono klienta." });
            }

            await _appointmentStatusService.RefreshExpiredAppointmentsAsync(cancellationToken);
            var allAppointments = await _appointments.GetAllAsync(cancellationToken);
            var hairdresserAppointmentsWithCustomer = allAppointments
                .Where(appointment => appointment.CustomerId == customerId && appointment.HairdresserId == user.HairdresserId)
                .OrderByDescending(appointment => appointment.StartAt)
                .ToList();

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
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może dodawać zdjęcie fryzjera." });
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
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może dodawać fryzjerów." });
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
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może edytować fryzjerów." });
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
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może usuwać fryzjerów." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono fryzjera do usunięcia." });
            }

            await DeleteHairdresserPresenceAsync(id, cancellationToken);
            await DeleteUsersLinkedWithHairdresserAsync(id, cancellationToken);
            return NoContent();
        }

        private static string? ValidateHairdresserStatusChange(Appointment appointment, AppointmentStatus status)
        {
            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                return "Nie można zmienić statusu anulowanej wizyty.";
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                return "Nie można zmienić statusu zakończonej wizyty.";
            }

            if (status != AppointmentStatus.Confirmed &&
                status != AppointmentStatus.Completed &&
                status != AppointmentStatus.Cancelled)
            {
                return "Fryzjer może ustawić status Confirmed, Completed albo Cancelled.";
            }

            if (status == AppointmentStatus.Completed && appointment.EndAt.ToUniversalTime() > DateTimeOffset.UtcNow)
            {
                return "Nie można zakończyć wizyty przed planowaną godziną zakończenia.";
            }

            return null;
        }

        private static Hairdresser Apply(Hairdresser hairdresser, HairdresserRequest request)
        {
            hairdresser.FirstName = request.FirstName;
            hairdresser.LastName = request.LastName;
            hairdresser.Specialization = request.Specialization;
            hairdresser.IsActive = request.IsActive;
            return hairdresser;
        }

        private async Task DeleteHairdresserPresenceAsync(string hairdresserId, CancellationToken cancellationToken)
        {
            var appointments = await _appointments.GetAllAsync(cancellationToken);
            foreach (var appointment in appointments.Where(appointment => appointment.HairdresserId == hairdresserId))
            {
                await _appointments.DeleteAsync(appointment.id, cancellationToken);
            }

            var reviews = await _reviews.GetAllAsync(cancellationToken);
            foreach (var review in reviews.Where(review => review.HairdresserId == hairdresserId))
            {
                await _reviews.DeleteAsync(review.id, cancellationToken);
            }

            await _repository.DeleteAsync(hairdresserId, cancellationToken);
        }

        private async Task DeleteUsersLinkedWithHairdresserAsync(string hairdresserId, CancellationToken cancellationToken)
        {
            var users = await _users.GetAllAsync(cancellationToken);
            foreach (var user in users.Where(user => user.HairdresserId == hairdresserId))
            {
                await _users.DeleteAsync(user.id, cancellationToken);
            }
        }
    }
}
