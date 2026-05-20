using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AdminController : ControllerBase
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IBookingRepository<AppUser> _users;
        private readonly IBookingRepository<Customer> _customers;
        private readonly IBookingRepository<Hairdresser> _hairdressers;
        private readonly IBookingRepository<SalonService> _services;
        private readonly IBookingRepository<SalonPhoto> _salonPhotos;
        private readonly IBookingRepository<Appointment> _appointments;
        private readonly IBookingRepository<Review> _reviews;
        private readonly IAppointmentStatusService _appointmentStatusService;

        public AdminController(
            ICurrentUserService currentUser,
            IBookingRepository<AppUser> users,
            IBookingRepository<Customer> customers,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services,
            IBookingRepository<SalonPhoto> salonPhotos,
            IBookingRepository<Appointment> appointments,
            IBookingRepository<Review> reviews,
            IAppointmentStatusService appointmentStatusService)
        {
            _currentUser = currentUser;
            _users = users;
            _customers = customers;
            _hairdressers = hairdressers;
            _services = services;
            _salonPhotos = salonPhotos;
            _appointments = appointments;
            _reviews = reviews;
            _appointmentStatusService = appointmentStatusService;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IReadOnlyList<AppUser>>> GetUsers(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić użytkowników." });
            }

            return Ok(await _users.GetAllAsync(cancellationToken));
        }

        [HttpGet("customers")]
        public async Task<ActionResult<IReadOnlyList<Customer>>> GetCustomers(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić klientów." });
            }

            return Ok(await _customers.GetAllAsync(cancellationToken));
        }

        [HttpGet("hairdressers")]
        public async Task<ActionResult<IReadOnlyList<Hairdresser>>> GetHairdressers(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić fryzjerów." });
            }

            return Ok(await _hairdressers.GetAllAsync(cancellationToken));
        }

        [HttpGet("services")]
        public async Task<ActionResult<IReadOnlyList<SalonService>>> GetServices(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić usługi." });
            }

            return Ok(await _services.GetAllAsync(cancellationToken));
        }

        [HttpGet("salon-photos")]
        public async Task<ActionResult<IReadOnlyList<SalonPhoto>>> GetSalonPhotos(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić zdjęcia salonu." });
            }

            return Ok(await _salonPhotos.GetAllAsync(cancellationToken));
        }

        [HttpGet("appointments")]
        public async Task<ActionResult<IReadOnlyList<Appointment>>> GetAppointments(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić wizyty." });
            }

            await _appointmentStatusService.RefreshExpiredAppointmentsAsync(cancellationToken);
            return Ok(await _appointments.GetAllAsync(cancellationToken));
        }

        [HttpGet("reviews")]
        public async Task<ActionResult<IReadOnlyList<Review>>> GetReviews(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić opinie." });
            }

            var reviews = await _reviews.GetAllAsync(cancellationToken);
            return Ok(reviews.OrderByDescending(review => review.CreatedAt).ToList());
        }

        [HttpPut("users/{userId}/role")]
        public async Task<ActionResult<AppUser>> AssignRole(string userId, AssignUserRoleRequest request, CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może zmieniać role użytkowników." });
            }

            var user = await _users.GetAsync(userId, cancellationToken);
            if (user is null)
            {
                return NotFound(new { error = "Nie znaleziono użytkownika." });
            }

            switch (request.Role)
            {
                case UserRole.Customer:
                    await AssignCustomerRoleAsync(user, cancellationToken);
                    break;
                case UserRole.Hairdresser:
                    await AssignHairdresserRoleAsync(user, request, cancellationToken);
                    break;
                case UserRole.Admin:
                    await DeleteLinkedProfileAsync(user, cancellationToken);
                    user.Role = UserRole.Admin;
                    user.CustomerId = null;
                    user.HairdresserId = null;
                    break;
                default:
                    return BadRequest(new { error = "Nieobsługiwana rola użytkownika." });
            }

            return Ok(await _users.UpsertAsync(user, cancellationToken));
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId, CancellationToken cancellationToken)
        {
            var currentUser = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (currentUser?.Role != UserRole.Admin)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może usuwać użytkowników." });
            }

            if (currentUser.id == userId)
            {
                return BadRequest(new { error = "Administrator nie może usunąć własnego konta z panelu administracyjnego." });
            }

            var user = await _users.GetAsync(userId, cancellationToken);
            if (user is null)
            {
                return NotFound(new { error = "Nie znaleziono użytkownika do usunięcia." });
            }

            await DeleteLinkedProfileAsync(user, cancellationToken);
            await _users.DeleteAsync(userId, cancellationToken);
            return NoContent();
        }

        private async Task AssignCustomerRoleAsync(AppUser user, CancellationToken cancellationToken)
        {
            var hairdresser = await GetHairdresserOrDefaultAsync(user.HairdresserId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(user.HairdresserId))
            {
                await DeleteHairdresserProfileAsync(user.HairdresserId, cancellationToken);
            }

            var existingCustomer = await GetCustomerOrDefaultAsync(user.CustomerId, cancellationToken);
            var customer = existingCustomer ?? CreateCustomerFromUser(user);
            if (hairdresser is not null)
            {
                customer.FirstName = FirstNotEmpty(hairdresser.FirstName, customer.FirstName, user.DisplayName);
                customer.LastName = FirstNotEmpty(hairdresser.LastName, customer.LastName);
            }

            customer.Email = FirstNotEmpty(customer.Email, user.Email);
            var savedCustomer = existingCustomer is null
                ? await _customers.CreateAsync(customer, cancellationToken)
                : await _customers.UpsertAsync(customer, cancellationToken);

            user.Role = UserRole.Customer;
            user.CustomerId = savedCustomer.id;
            user.HairdresserId = null;
        }

        private async Task AssignHairdresserRoleAsync(AppUser user, AssignUserRoleRequest request, CancellationToken cancellationToken)
        {
            var customer = await GetCustomerOrDefaultAsync(user.CustomerId, cancellationToken);
            var existingHairdresser = await GetHairdresserOrDefaultAsync(user.HairdresserId, cancellationToken);
            var hairdresser = existingHairdresser ?? CreateHairdresserFromUser(user, request.Specialization);

            if (customer is not null)
            {
                hairdresser.FirstName = FirstNotEmpty(customer.FirstName, hairdresser.FirstName, user.DisplayName);
                hairdresser.LastName = FirstNotEmpty(customer.LastName, hairdresser.LastName);
                hairdresser.Specialization = FirstNotEmpty(request.Specialization, hairdresser.Specialization, "Fryzjer");
            }

            hairdresser.IsActive = true;
            var savedHairdresser = existingHairdresser is null
                ? await _hairdressers.CreateAsync(hairdresser, cancellationToken)
                : await _hairdressers.UpsertAsync(hairdresser, cancellationToken);

            if (!string.IsNullOrWhiteSpace(user.CustomerId))
            {
                await DeleteCustomerProfileAsync(user.CustomerId, cancellationToken);
            }

            user.Role = UserRole.Hairdresser;
            user.CustomerId = null;
            user.HairdresserId = savedHairdresser.id;
        }

        private async Task DeleteLinkedProfileAsync(AppUser user, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(user.CustomerId))
            {
                await DeleteCustomerProfileAsync(user.CustomerId, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(user.HairdresserId))
            {
                await DeleteHairdresserProfileAsync(user.HairdresserId, cancellationToken);
            }
        }

        private async Task DeleteCustomerProfileAsync(string customerId, CancellationToken cancellationToken)
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

            if (await _customers.GetAsync(customerId, cancellationToken) is not null)
            {
                await _customers.DeleteAsync(customerId, cancellationToken);
            }
        }

        private async Task DeleteHairdresserProfileAsync(string hairdresserId, CancellationToken cancellationToken)
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

            if (await _hairdressers.GetAsync(hairdresserId, cancellationToken) is not null)
            {
                await _hairdressers.DeleteAsync(hairdresserId, cancellationToken);
            }
        }

        private async Task<Customer?> GetCustomerOrDefaultAsync(string? customerId, CancellationToken cancellationToken)
        {
            return string.IsNullOrWhiteSpace(customerId) ? null : await _customers.GetAsync(customerId, cancellationToken);
        }

        private async Task<Hairdresser?> GetHairdresserOrDefaultAsync(string? hairdresserId, CancellationToken cancellationToken)
        {
            return string.IsNullOrWhiteSpace(hairdresserId) ? null : await _hairdressers.GetAsync(hairdresserId, cancellationToken);
        }

        private static Customer CreateCustomerFromUser(AppUser user)
        {
            var name = SplitDisplayName(user.DisplayName);
            return new Customer
            {
                FirstName = name.FirstName,
                LastName = name.LastName,
                Email = user.Email,
                PhoneNumber = string.Empty,
                Notes = "Utworzono automatycznie podczas nadawania roli klienta."
            };
        }

        private static Hairdresser CreateHairdresserFromUser(AppUser user, string? specialization)
        {
            var name = SplitDisplayName(user.DisplayName);
            return new Hairdresser
            {
                FirstName = name.FirstName,
                LastName = name.LastName,
                Specialization = FirstNotEmpty(specialization, "Fryzjer"),
                IsActive = true
            };
        }

        private static (string FirstName, string LastName) SplitDisplayName(string displayName)
        {
            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length switch
            {
                0 => (string.Empty, string.Empty),
                1 => (parts[0], string.Empty),
                _ => (parts[0], string.Join(" ", parts.Skip(1)))
            };
        }

        private static string FirstNotEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private async Task<bool> IsAdminAsync(CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            return user?.Role == UserRole.Admin;
        }
    }
}
