using HairSalon.Booking.Api.Contracts;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using HairSalon.Booking.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AppointmentsController(
    IBookingRepository<Appointment> repository,
    IAppointmentBookingFacade bookingFacade) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Appointment>>> GetAll(CancellationToken cancellationToken)
        => Ok(await repository.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> GetById(string id, CancellationToken cancellationToken)
    {
        var appointment = await repository.GetAsync(id, cancellationToken);
        return appointment is null ? NotFound() : Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> Create(AppointmentRequest request, CancellationToken cancellationToken)
    {
        var appointment = Apply(new Appointment(), request);
        try
        {
            var created = await bookingFacade.BookAsync(appointment, cancellationToken);
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
        var existing = await repository.GetAsync(id, cancellationToken);
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

    private static Appointment Apply(Appointment appointment, AppointmentRequest request)
    {
        appointment.CustomerId = request.CustomerId;
        appointment.HairdresserId = request.HairdresserId;
        appointment.SalonServiceId = request.SalonServiceId;
        appointment.StartAt = request.StartAt;
        appointment.Status = request.Status;
        appointment.Notes = request.Notes;
        return appointment;
    }
}
