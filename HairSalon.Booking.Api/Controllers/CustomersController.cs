using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CustomersController(
    IBookingRepository<Customer> repository,
    IBookingRepository<Appointment> appointments,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Customer>>> GetAll(CancellationToken cancellationToken)
        => Ok(await repository.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetById(string id, CancellationToken cancellationToken)
    {
        var customer = await repository.GetAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpGet("me")]
    public async Task<ActionResult<Customer>> GetMe(CancellationToken cancellationToken)
    {
        var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
        if (user?.CustomerId is null)
        {
            return Forbid();
        }

        var customer = await repository.GetAsync(user.CustomerId, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpGet("me/appointments")]
    public async Task<ActionResult<IReadOnlyList<Appointment>>> GetMyAppointments(CancellationToken cancellationToken)
    {
        var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
        if (user?.CustomerId is null)
        {
            return Forbid();
        }

        var allAppointments = await appointments.GetAllAsync(cancellationToken);
        var customerAppointments = allAppointments
            .Where(appointment => appointment.CustomerId == user.CustomerId)
            .OrderByDescending(appointment => appointment.StartAt)
            .ToList();

        return Ok(customerAppointments);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create(CustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = Apply(new Customer(), request);
        var created = await repository.CreateAsync(customer, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Customer>> Update(string id, CustomerRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var updated = await repository.UpsertAsync(Apply(existing, request), cancellationToken);
        return Ok(updated);
    }

    [HttpPut("me")]
    public async Task<ActionResult<Customer>> UpdateMe(CustomerRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
        if (user?.CustomerId is null)
        {
            return Forbid();
        }

        var existing = await repository.GetAsync(user.CustomerId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var updated = await repository.UpsertAsync(Apply(existing, request), cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(id, cancellationToken);
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
}
