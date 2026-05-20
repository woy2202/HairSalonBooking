using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Model;
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
        private readonly IBookingRepository<AppUser> _users;
        private readonly IBookingRepository<Appointment> _appointments;
        private readonly IBookingRepository<Hairdresser> _hairdressers;
        private readonly IBookingRepository<SalonService> _services;
        private readonly IBookingRepository<Review> _reviews;
        private readonly ICurrentUserService _currentUser;
        private readonly IAppointmentStatusService _appointmentStatusService;

        public CustomersController(
            IBookingRepository<Customer> repository,
            IBookingRepository<AppUser> users,
            IBookingRepository<Appointment> appointments,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services,
            IBookingRepository<Review> reviews,
            ICurrentUserService currentUser,
            IAppointmentStatusService appointmentStatusService)
        {
            _repository = repository;
            _users = users;
            _appointments = appointments;
            _hairdressers = hairdressers;
            _services = services;
            _reviews = reviews;
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

        [HttpPatch("me/appointments/{appointmentId}/cancel")]
        public async Task<ActionResult<Appointment>> CancelMyAppointment(string appointmentId, CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.CustomerId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu klienta." });
            }

            var appointment = await _appointments.GetAsync(appointmentId, cancellationToken);
            if (appointment is null)
            {
                return NotFound(new { error = "Nie znaleziono wizyty do odwołania." });
            }

            appointment = await _appointmentStatusService.RefreshExpiredAppointmentAsync(appointment, cancellationToken);
            if (appointment.CustomerId != user.CustomerId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Klient może odwołać tylko swoją wizytę." });
            }

            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                return BadRequest(new { error = "Ta wizyta jest już odwołana." });
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                return BadRequest(new { error = "Nie można odwołać wizyty, która już się zakończyła." });
            }

            appointment.Status = AppointmentStatus.Cancelled;
            return Ok(await _appointments.UpsertAsync(appointment, cancellationToken));
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

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user is null || user.Role != UserRole.Customer)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko klient może usunąć swoje konto klienta." });
            }

            if (!string.IsNullOrWhiteSpace(user.CustomerId))
            {
                await DeleteCustomerPresenceAsync(user.CustomerId, cancellationToken);
            }

            await _users.DeleteAsync(user.id, cancellationToken);
            return NoContent();
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

            await DeleteCustomerPresenceAsync(id, cancellationToken);
            await DeleteUsersLinkedWithCustomerAsync(id, cancellationToken);
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

        private async Task DeleteCustomerPresenceAsync(string customerId, CancellationToken cancellationToken)
        {
            var appointments = await _appointments.GetAllAsync(cancellationToken);
            foreach (var appointment in appointments.Where(appointment => appointment.CustomerId == customerId))
            {
                await _appointments.DeleteAsync(appointment.id, cancellationToken);
            }

            var reviews = await _reviews.GetAllAsync(cancellationToken);
            foreach (var review in reviews.Where(review => review.CustomerId == customerId))
            {
                await _reviews.DeleteAsync(review.id, cancellationToken);
            }

            await _repository.DeleteAsync(customerId, cancellationToken);
        }

        private async Task DeleteUsersLinkedWithCustomerAsync(string customerId, CancellationToken cancellationToken)
        {
            var users = await _users.GetAllAsync(cancellationToken);
            foreach (var user in users.Where(user => user.CustomerId == customerId))
            {
                await _users.DeleteAsync(user.id, cancellationToken);
            }
        }
    }
}
