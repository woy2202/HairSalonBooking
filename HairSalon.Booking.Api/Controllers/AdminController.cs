using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AdminController(
    ICurrentUserService currentUser,
    IBookingRepository<AppUser> users,
    IBookingRepository<Customer> customers,
    IBookingRepository<Hairdresser> hairdressers,
    IBookingRepository<Appointment> appointments) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> GetUsers(CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        return Ok(await users.GetAllAsync(cancellationToken));
    }

    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<Customer>>> GetCustomers(CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        return Ok(await customers.GetAllAsync(cancellationToken));
    }

    [HttpGet("hairdressers")]
    public async Task<ActionResult<IReadOnlyList<Hairdresser>>> GetHairdressers(CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        return Ok(await hairdressers.GetAllAsync(cancellationToken));
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<IReadOnlyList<Appointment>>> GetAppointments(CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        return Ok(await appointments.GetAllAsync(cancellationToken));
    }

    [HttpPut("users/{userId}/role")]
    public async Task<ActionResult<AppUser>> AssignRole(string userId, AssignUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var user = await users.GetAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        switch (request.Role)
        {
            case UserRole.Customer:
                await AssignCustomerRoleAsync(user, request.CustomerId, cancellationToken);
                break;
            case UserRole.Hairdresser:
                var hairdresserId = request.HairdresserId;
                if (string.IsNullOrWhiteSpace(hairdresserId) || await hairdressers.GetAsync(hairdresserId, cancellationToken) is null)
                {
                    return BadRequest(new { error = "HairdresserId is required and must point to an existing hairdresser." });
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
                return BadRequest(new { error = "Unsupported role." });
        }

        return Ok(await users.UpsertAsync(user, cancellationToken));
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

        var customer = await customers.CreateAsync(new Customer
        {
            FirstName = user.DisplayName,
            Email = user.Email,
            PhoneNumber = string.Empty,
            Notes = "Created automatically while assigning Customer role."
        }, cancellationToken);

        user.CustomerId = customer.id;
    }

    private async Task<bool> IsAdminAsync(CancellationToken cancellationToken)
    {
        var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
        return user?.Role == UserRole.Admin;
    }
}
