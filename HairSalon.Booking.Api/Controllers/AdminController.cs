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

        public AdminController(
            ICurrentUserService currentUser,
            IBookingRepository<AppUser> users,
            IBookingRepository<Customer> customers,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services,
            IBookingRepository<SalonPhoto> salonPhotos,
            IBookingRepository<Appointment> appointments)
        {
            _currentUser = currentUser;
            _users = users;
            _customers = customers;
            _hairdressers = hairdressers;
            _services = services;
            _salonPhotos = salonPhotos;
            _appointments = appointments;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IReadOnlyList<AppUser>>> GetUsers(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator moze wyswietlić użytkowników." });
            }

            return Ok(await _users.GetAllAsync(cancellationToken));
        }

        [HttpGet("customers")]
        public async Task<ActionResult<IReadOnlyList<Customer>>> GetCustomers(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator moze wyswietlić klientów." });
            }

            return Ok(await _customers.GetAllAsync(cancellationToken));
        }

        [HttpGet("hairdressers")]
        public async Task<ActionResult<IReadOnlyList<Hairdresser>>> GetHairdressers(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator moze wyswietlić fryzjerów." });
            }

            return Ok(await _hairdressers.GetAllAsync(cancellationToken));
        }

        [HttpGet("services")]
        public async Task<ActionResult<IReadOnlyList<SalonService>>> GetServices(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator moze wyswietlic uslugi." });
            }

            return Ok(await _services.GetAllAsync(cancellationToken));
        }

        [HttpGet("salon-photos")]
        public async Task<ActionResult<IReadOnlyList<SalonPhoto>>> GetSalonPhotos(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator moze wyswietlic zdjecia salonu." });
            }

            return Ok(await _salonPhotos.GetAllAsync(cancellationToken));
        }

        [HttpGet("appointments")]
        public async Task<ActionResult<IReadOnlyList<Appointment>>> GetAppointments(CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator moze wyswietlić wizyty." });
            }

            return Ok(await _appointments.GetAllAsync(cancellationToken));
        }

        [HttpPut("users/{userId}/role")]
        public async Task<ActionResult<AppUser>> AssignRole(string userId, AssignUserRoleRequest request, CancellationToken cancellationToken)
        {
            if (!await IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator moze zmieniać role użytkowników." });
            }

            var user = await _users.GetAsync(userId, cancellationToken);
            if (user is null)
            {
                return NotFound(new { error = "Nie znaleziono użytkownika." });
            }

            switch (request.Role)
            {
                case UserRole.Customer:
                    await AssignCustomerRoleAsync(user, request.CustomerId, cancellationToken);
                    break;
                case UserRole.Hairdresser:
                    var hairdresserId = request.HairdresserId;
                    if (string.IsNullOrWhiteSpace(hairdresserId) || await _hairdressers.GetAsync(hairdresserId, cancellationToken) is null)
                    {
                        return BadRequest(new { error = "HairdresserId jest wymagane i musi wskazywać istniejącego fryzjera." });
                    }

                    user.Role = UserRole.Hairdresser;
                    user.HairdresserId = hairdresserId;
                    user.CustomerId = null;
                    break;
                case UserRole.Admin:
                    user.Role = UserRole.Admin;
                    user.CustomerId = null;
                    user.HairdresserId = null;
                    break;
                default:
                    return BadRequest(new { error = "Nieobsługiwana rola użytkownika." });
            }

            return Ok(await _users.UpsertAsync(user, cancellationToken));
        }

        private async Task AssignCustomerRoleAsync(AppUser user, string? customerId, CancellationToken cancellationToken)
        {
            user.Role = UserRole.Customer;
            user.HairdresserId = null;

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                user.CustomerId = customerId;
                return;
            }

            if (!string.IsNullOrWhiteSpace(user.CustomerId))
            {
                return;
            }

            var customer = await _customers.CreateAsync(new Customer
            {
                FirstName = user.DisplayName,
                Email = user.Email,
                PhoneNumber = string.Empty,
                Notes = "Utworzono automatycznie podczas nadawania roli klienta."
            }, cancellationToken);

            user.CustomerId = customer.id;
        }

        private async Task<bool> IsAdminAsync(CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            return user?.Role == UserRole.Admin;
        }
    }
}
