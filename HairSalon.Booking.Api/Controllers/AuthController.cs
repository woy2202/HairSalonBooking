using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController : ControllerBase
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IBookingRepository<AppUser> _users;
        private readonly IBookingRepository<Customer> _customers;

        public AuthController(
            ICurrentUserService currentUser,
            IBookingRepository<AppUser> users,
            IBookingRepository<Customer> customers)
        {
            _currentUser = currentUser;
            _users = users;
            _customers = customers;
        }

        [HttpGet("me")]
        public async Task<ActionResult> Me(CancellationToken cancellationToken)
        {
            var principal = _currentUser.GetCurrentPrincipal();
            if (principal is null)
            {
                return Unauthorized(new { error = "Brakuje nagłówków zalogowanego użytkownika z Easy Auth." });
            }

            var user = await _users.GetAsync(principal.LocalUserId, cancellationToken);
            Customer? customer = null;
            if (!string.IsNullOrWhiteSpace(user?.CustomerId))
            {
                customer = await _customers.GetAsync(user.CustomerId, cancellationToken);
            }

            return Ok(new
            {
                principal.Provider,
                principal.ProviderUserId,
                principal.Name,
                principal.Email,
                User = user,
                Customer = customer
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<AppUser>> RegisterOrUpdateProfile(AuthProfileRequest request, CancellationToken cancellationToken)
        {
            var principal = _currentUser.GetCurrentPrincipal();
            if (principal is null)
            {
                return Unauthorized(new { error = "Brakuje nagłówków zalogowanego użytkownika z Easy Auth." });
            }

            var existing = await _users.GetAsync(principal.LocalUserId, cancellationToken);
            var user = existing ?? new AppUser
            {
                id = principal.LocalUserId,
                Provider = principal.Provider,
                ProviderUserId = principal.ProviderUserId,
                Role = UserRole.Customer,
                CreatedAt = DateTimeOffset.UtcNow
            };

            user.DisplayName = FirstNotEmpty(request.DisplayName, principal.Name, user.DisplayName);
            user.Email = FirstNotEmpty(request.Email, principal.Email, user.Email);
            user.LastLoginAt = DateTimeOffset.UtcNow;

            if (user.Role == UserRole.Customer)
            {
                Customer? customer = null;
                if (!string.IsNullOrWhiteSpace(user.CustomerId))
                {
                    customer = await _customers.GetAsync(user.CustomerId, cancellationToken);
                }

                var hasSavedCustomer = customer is not null;
                customer ??= new Customer();
                ApplyCustomerProfile(customer, request, user);

                var savedCustomer = hasSavedCustomer
                    ? await _customers.UpsertAsync(customer, cancellationToken)
                    : await _customers.CreateAsync(customer, cancellationToken);

                user.CustomerId = savedCustomer.id;
                user.HairdresserId = null;
            }

            var saved = existing is null
                ? await _users.CreateAsync(user, cancellationToken)
                : await _users.UpsertAsync(user, cancellationToken);

            return Ok(saved);
        }

        [HttpGet("headers")]
        public ActionResult Headers()
        {
            return Ok(new
            {
                Name = Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].ToString(),
                Id = Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].ToString(),
                Provider = Request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"].ToString(),
                HasEncodedPrincipal = Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL")
            });
        }

        private static void ApplyCustomerProfile(Customer customer, AuthProfileRequest request, AppUser user)
        {
            var name = SplitDisplayName(user.DisplayName);
            customer.FirstName = FirstNotEmpty(customer.FirstName, name.FirstName, user.DisplayName);
            customer.LastName = FirstNotEmpty(customer.LastName, name.LastName);
            customer.PhoneNumber = FirstNotEmpty(customer.PhoneNumber);
            customer.Email = FirstNotEmpty(request.Email, customer.Email, user.Email);

            if (string.IsNullOrWhiteSpace(customer.Notes))
            {
                customer.Notes = "Utworzono automatycznie podczas rejestracji przez Google.";
            }
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
    }
}
