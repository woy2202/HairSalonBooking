using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using HairSalon.Booking.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HairdressersController(
    IBookingRepository<Hairdresser> repository,
    IBookingRepository<Appointment> appointments,
    IBookingRepository<Customer> customers,
    IAppointmentBookingFacade bookingFacade,
    IPhotoStorageService photoStorage,
    ICurrentUserService currentUser,
    IOptions<AzureBookingOptions> options) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Hairdresser>>> GetAll(CancellationToken cancellationToken)
        => Ok(await repository.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<Hairdresser>> GetById(string id, CancellationToken cancellationToken)
    {
        var hairdresser = await repository.GetAsync(id, cancellationToken);
        return hairdresser is null ? NotFound() : Ok(hairdresser);
    }

    [HttpGet("me/appointments")]
    public async Task<ActionResult<IReadOnlyList<Appointment>>> GetMyAppointments(CancellationToken cancellationToken)
    {
        var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
        if (user?.HairdresserId is null)
        {
            return Forbid();
        }

        var allAppointments = await appointments.GetAllAsync(cancellationToken);
        var hairdresserAppointments = allAppointments
            .Where(appointment => appointment.HairdresserId == user.HairdresserId)
            .OrderBy(appointment => appointment.StartAt)
            .ToList();

        return Ok(hairdresserAppointments);
    }

    [HttpGet("me/customers/{customerId}/history")]
    public async Task<ActionResult> GetCustomerHistory(string customerId, CancellationToken cancellationToken)
    {
        var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
        if (user?.HairdresserId is null)
        {
            return Forbid();
        }

        var customer = await customers.GetAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var allAppointments = await appointments.GetAllAsync(cancellationToken);
        var sharedAppointments = allAppointments
            .Where(appointment => appointment.CustomerId == customerId && appointment.HairdresserId == user.HairdresserId)
            .OrderByDescending(appointment => appointment.StartAt)
            .ToList();

        if (sharedAppointments.Count == 0)
        {
            return Forbid();
        }

        return Ok(new
        {
            Customer = customer,
            PreviousAppointments = sharedAppointments.Where(appointment => appointment.StartAt < DateTimeOffset.UtcNow),
            UpcomingAppointments = sharedAppointments.Where(appointment => appointment.StartAt >= DateTimeOffset.UtcNow),
            UsedServiceIds = sharedAppointments.Select(appointment => appointment.SalonServiceId).Distinct().ToList()
        });
    }

    [HttpGet("{id}/availability/{day}")]
    public async Task<ActionResult<IReadOnlyList<DateTimeOffset>>> GetAvailability(string id, DateOnly day, CancellationToken cancellationToken)
        => Ok(await bookingFacade.GetAvailableSlotsAsync(id, day, cancellationToken));

    [HttpPost("{id}/photo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Hairdresser>> UploadPhoto(string id, IFormFile file, CancellationToken cancellationToken)
    {
        var hairdresser = await repository.GetAsync(id, cancellationToken);
        if (hairdresser is null)
        {
            return NotFound();
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(hairdresser.PhotoBlobName))
            {
                await photoStorage.DeleteAsync(options.Value.Storage.HairdresserPhotoBlobContainer, hairdresser.PhotoBlobName, cancellationToken);
            }

            var uploaded = await photoStorage.UploadAsync(options.Value.Storage.HairdresserPhotoBlobContainer, file, cancellationToken);
            hairdresser.PhotoUrl = uploaded.Url;
            hairdresser.PhotoBlobName = uploaded.BlobName;

            return Ok(await repository.UpsertAsync(hairdresser, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Hairdresser>> Create(HairdresserRequest request, CancellationToken cancellationToken)
    {
        var hairdresser = Apply(new Hairdresser(), request);
        var created = await repository.CreateAsync(hairdresser, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Hairdresser>> Update(string id, HairdresserRequest request, CancellationToken cancellationToken)
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

    private static Hairdresser Apply(Hairdresser hairdresser, HairdresserRequest request)
    {
        hairdresser.FirstName = request.FirstName;
        hairdresser.LastName = request.LastName;
        hairdresser.Specialization = request.Specialization;
        hairdresser.IsActive = request.IsActive;
        return hairdresser;
    }
}
