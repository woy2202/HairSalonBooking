using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class CustomersController : ControllerBase
    {
        private readonly IBookingRepository<Customer> _repository;
        private readonly IBookingRepository<Appointment> _appointments;
        private readonly IBookingRepository<Hairdresser> _hairdressers;
        private readonly IBookingRepository<SalonService> _services;
        private readonly ICurrentUserService _currentUser;
        private readonly IAppointmentStatusService _appointmentStatusService;

        public CustomersController(
            IBookingRepository<Customer> repository,
            IBookingRepository<Appointment> appointments,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services,
            ICurrentUserService currentUser,
            IAppointmentStatusService appointmentStatusService)
        {
            _repository = repository;
            _appointments = appointments;
            _hairdressers = hairdressers;
            _services = services;
            _currentUser = currentUser;
            _appointmentStatusService = appointmentStatusService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Customer>>> GetAll(CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić listę klientów." });
            }

            var customers = await _repository.GetAllAsync(cancellationToken);
            return Ok(customers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetById(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić dowolnego klienta po id." });
            }

            var customer = await _repository.GetAsync(id, cancellationToken);
            return customer is null ? NotFound(new { error = "Nie znaleziono klienta." }) : Ok(customer);
        }

        [HttpGet("me")]
        public async Task<ActionResult<Customer>> GetMe(CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.CustomerId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu klienta." });
            }

            var customer = await _repository.GetAsync(user.CustomerId, cancellationToken);
            return customer is null ? NotFound(new { error = "Nie znaleziono Twojego profilu klienta." }) : Ok(customer);
        }

        [HttpGet("me/appointments")]
        public async Task<ActionResult<IReadOnlyList<Appointment>>> GetMyAppointments(CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.CustomerId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu klienta." });
            }

            await _appointmentStatusService.RefreshExpiredAppointmentsAsync(cancellationToken);
            var allAppointments = await _appointments.GetAllAsync(cancellationToken);
            var customerAppointments = allAppointments
                .Where(appointment => appointment.CustomerId == user.CustomerId)
                .OrderByDescending(appointment => appointment.StartAt)
                .ToList();

            return Ok(customerAppointments);
        }

        [HttpGet("{id}/history")]
        public async Task<ActionResult> GetHistory(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić pełną historię klienta." });
            }

            var customer = await _repository.GetAsync(id, cancellationToken);
            if (customer is null)
            {
                return NotFound(new { error = "Nie znaleziono klienta." });
            }

            await _appointmentStatusService.RefreshExpiredAppointmentsAsync(cancellationToken);
            var appointments = (await _appointments.GetAllAsync(cancellationToken))
                .Where(appointment => appointment.CustomerId == id)
                .OrderByDescending(appointment => appointment.StartAt)
                .ToList();
            var hairdressers = await _hairdressers.GetAllAsync(cancellationToken);
            var services = await _services.GetAllAsync(cancellationToken);

            var history = appointments.Select(appointment => new
            {
                Appointment = appointment,
                Hairdresser = hairdressers.FirstOrDefault(hairdresser => hairdresser.id == appointment.HairdresserId),
                Service = services.FirstOrDefault(service => service.id == appointment.SalonServiceId)
            }).ToList();

            return Ok(new
            {
                Customer = customer,
                History = history
            });
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> Create(CustomerRequest request, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może dodawać klientów." });
            }

            var customer = Apply(new Customer(), request);
            var created = await _repository.CreateAsync(customer, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Customer>> Update(string id, CustomerRequest request, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może edytować dowolnego klienta." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono klienta do edycji." });
            }

            var updated = await _repository.UpsertAsync(Apply(existing, request), cancellationToken);
            return Ok(updated);
        }

        [HttpPut("me")]
        public async Task<ActionResult<Customer>> UpdateMe(CustomerRequest request, CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.CustomerId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu klienta." });
            }

            var existing = await _repository.GetAsync(user.CustomerId, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono Twojego profilu klienta." });
            }

            var updated = await _repository.UpsertAsync(Apply(existing, request), cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może usuwać klientów." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono klienta do usunięcia." });
            }

            if (await HasAppointmentsAsync(id, cancellationToken))
            {
                return BadRequest(new { error = "Nie można usunąć klienta, ponieważ ma przypisane wizyty. Najpierw anuluj albo usuń powiązane wizyty." });
            }

            await _repository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        private static Customer Apply(Customer customer, CustomerRequest request)
        {
            customer.FirstName = request.FirstName;
            customer.LastName = request.LastName;
            customer.PhoneNumber = request.PhoneNumber;
            customer.Email = request.Email;
            customer.Notes = request.Notes;
            return customer;
        }

        private async Task<bool> HasAppointmentsAsync(string customerId, CancellationToken cancellationToken)
        {
            var appointments = await _appointments.GetAllAsync(cancellationToken);
            return appointments.Any(appointment => appointment.CustomerId == customerId);
        }
    }
}
