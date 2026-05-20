using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using HairSalon.Booking.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AppointmentsController : ControllerBase
    {
        private static readonly TimeOnly SalonOpeningTime = new TimeOnly(9, 0);
        private static readonly TimeOnly SalonClosingTime = new TimeOnly(17, 0);
        private const int SlotMinutes = 30;

        private readonly IBookingRepository<Appointment> _repository;
        private readonly IBookingRepository<Customer> _customers;
        private readonly IBookingRepository<Hairdresser> _hairdressers;
        private readonly IBookingRepository<SalonService> _services;
        private readonly IAppointmentBookingFacade _bookingFacade;
        private readonly ICurrentUserService _currentUser;
        private readonly IAppointmentStatusService _appointmentStatusService;

        public AppointmentsController(
            IBookingRepository<Appointment> repository,
            IBookingRepository<Customer> customers,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services,
            IAppointmentBookingFacade bookingFacade,
            ICurrentUserService currentUser,
            IAppointmentStatusService appointmentStatusService)
        {
            _repository = repository;
            _customers = customers;
            _hairdressers = hairdressers;
            _services = services;
            _bookingFacade = bookingFacade;
            _currentUser = currentUser;
            _appointmentStatusService = appointmentStatusService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Appointment>>> GetAll(CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić wszystkie wizyty." });
            }

            await _appointmentStatusService.RefreshExpiredAppointmentsAsync(cancellationToken);
            var appointments = await _repository.GetAllAsync(cancellationToken);
            return Ok(appointments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Appointment>> GetById(string id, CancellationToken cancellationToken)
        {
            var appointment = await _repository.GetAsync(id, cancellationToken);
            if (appointment is null)
            {
                return NotFound(new { error = "Nie znaleziono wizyty." });
            }

            appointment = await _appointmentStatusService.RefreshExpiredAppointmentAsync(appointment, cancellationToken);
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (!CanReadAppointment(user, appointment))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Nie masz uprawnień do odczytu tej wizyty." });
            }

            return Ok(appointment);
        }

        [HttpPost]
        public async Task<ActionResult<Appointment>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (!CanCreateAppointment(user, request))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Klient może utworzyć wizytę tylko dla swojego konta. Administrator może utworzyć dowolną wizytę." });
            }

            var appointment = Apply(new Appointment(), request);
            try
            {
                var created = await _bookingFacade.BookAsync(appointment, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Appointment>> Update(string id, AppointmentRequest request, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może edytować wizyty." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono wizyty do edycji." });
            }

            var validationError = await ValidateAdminUpdateAsync(id, request, cancellationToken);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return BadRequest(new { error = validationError });
            }

            var updated = await _repository.UpsertAsync(await ApplyAsync(existing, request, cancellationToken), cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może usuwać wizyty." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono wizyty do usunięcia." });
            }

            await _repository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        private static Appointment Apply(Appointment appointment, CreateAppointmentRequest request)
        {
            appointment.CustomerId = request.CustomerId;
            appointment.HairdresserId = request.HairdresserId;
            appointment.SalonServiceId = request.SalonServiceId;
            appointment.StartAt = request.StartAt;
            appointment.Status = AppointmentStatus.Booked;
            appointment.Notes = request.Notes;
            return appointment;
        }

        private static Appointment Apply(Appointment appointment, AppointmentRequest request)
        {
            appointment.CustomerId = request.CustomerId;
            appointment.HairdresserId = request.HairdresserId;
            appointment.SalonServiceId = request.SalonServiceId;
            appointment.StartAt = request.StartAt;
            appointment.Notes = request.Notes;
            appointment.Status = request.Status;
            return appointment;
        }

        private async Task<Appointment> ApplyAsync(Appointment appointment, AppointmentRequest request, CancellationToken cancellationToken)
        {
            var service = await _services.GetAsync(request.SalonServiceId, cancellationToken);
            appointment = Apply(appointment, request);

            if (service is not null)
            {
                appointment.EndAt = appointment.StartAt.AddMinutes(service.DurationMinutes);
            }

            return appointment;
        }

        private async Task<string?> ValidateAdminUpdateAsync(string appointmentId, AppointmentRequest request, CancellationToken cancellationToken)
        {
            if (await _customers.GetAsync(request.CustomerId, cancellationToken) is null)
            {
                return "Klient przypisany do wizyty nie istnieje.";
            }

            var hairdresser = await _hairdressers.GetAsync(request.HairdresserId, cancellationToken);
            if (hairdresser is null || !hairdresser.IsActive)
            {
                return "Fryzjer przypisany do wizyty nie istnieje albo jest nieaktywny.";
            }

            var service = await _services.GetAsync(request.SalonServiceId, cancellationToken);
            if (service is null || !service.IsAvailable)
            {
                return "Usługa przypisana do wizyty nie istnieje albo jest niedostępna.";
            }

            var endAt = request.StartAt.AddMinutes(service.DurationMinutes);
            var timeValidationError = ValidateAppointmentTime(request.StartAt, endAt);
            if (!string.IsNullOrWhiteSpace(timeValidationError))
            {
                return timeValidationError;
            }

            var allAppointments = await _repository.GetAllAsync(cancellationToken);
            var hasCollision = allAppointments.Any(existing =>
                existing.id != appointmentId &&
                existing.HairdresserId == request.HairdresserId &&
                existing.Status != AppointmentStatus.Cancelled &&
                request.StartAt < existing.EndAt &&
                endAt > existing.StartAt);

            return hasCollision ? "Wybrany termin wizyty jest już zajęty." : null;
        }

        private static string? ValidateAppointmentTime(DateTimeOffset startAt, DateTimeOffset endAt)
        {
            if (startAt == default)
            {
                return "Data rozpoczęcia wizyty jest wymagana.";
            }

            if (startAt.ToUniversalTime() <= DateTimeOffset.UtcNow)
            {
                return "Nie można ustawić wizyty w przeszłości.";
            }

            if (startAt.Minute % SlotMinutes != 0 || startAt.Second != 0)
            {
                return "Wizyta musi zaczynać się o pełnej półgodzinie, np. 09:00 albo 09:30.";
            }

            var startTime = TimeOnly.FromDateTime(startAt.DateTime);
            var endTime = TimeOnly.FromDateTime(endAt.DateTime);

            if (startTime < SalonOpeningTime || endTime > SalonClosingTime || endAt.Date != startAt.Date)
            {
                return "Wizyta musi mieścić się w godzinach pracy salonu od 09:00 do 17:00.";
            }

            return null;
        }

        private static bool CanReadAppointment(AppUser? user, Appointment appointment)
        {
            if (user is null)
            {
                return false;
            }

            if (user.Role == UserRole.Admin)
            {
                return true;
            }

            if (user.Role == UserRole.Customer)
            {
                return appointment.CustomerId == user.CustomerId;
            }

            if (user.Role == UserRole.Hairdresser)
            {
                return appointment.HairdresserId == user.HairdresserId;
            }

            return false;
        }

        private static bool CanCreateAppointment(AppUser? user, CreateAppointmentRequest request)
        {
            if (user is null)
            {
                return false;
            }

            if (user.Role == UserRole.Admin)
            {
                return true;
            }

            return user.Role == UserRole.Customer &&
                !string.IsNullOrWhiteSpace(user.CustomerId) &&
                request.CustomerId == user.CustomerId;
        }
    }
}
