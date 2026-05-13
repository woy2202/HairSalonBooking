using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    ICurrentUserService currentUser,
    IBookingRepository<AppUser> users,
    IBookingRepository<Customer> customers) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult> Me(CancellationToken cancellationToken)
    {
        var principal = currentUser.GetCurrentPrincipal();
        if (principal is null)
        {
            return Unauthorized(new { error = "Easy Auth user headers are missing." });
        }

        var user = await users.GetAsync(principal.LocalUserId, cancellationToken);
        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(user?.CustomerId))
        {
            customer = await customers.GetAsync(user.CustomerId, cancellationToken);
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
        var principal = currentUser.GetCurrentPrincipal();
        if (principal is null)
        {
            return Unauthorized(new { error = "Easy Auth user headers are missing." });
        }

        var existing = await users.GetAsync(principal.LocalUserId, cancellationToken);
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

        if (user.Role == UserRole.Customer && string.IsNullOrWhiteSpace(user.CustomerId))
        {
            var customer = await customers.CreateAsync(new Customer
            {
                FirstName = user.DisplayName,
                LastName = string.Empty,
                Email = user.Email,
                PhoneNumber = string.Empty,
                Notes = "Created automatically from Easy Auth registration."
            }, cancellationToken);

            user.CustomerId = customer.id;
        }

        var saved = existing is null
            ? await users.CreateAsync(user, cancellationToken)
            : await users.UpsertAsync(user, cancellationToken);

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

    private static string FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
