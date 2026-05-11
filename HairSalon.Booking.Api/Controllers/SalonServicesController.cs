using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SalonServicesController(IBookingRepository<SalonService> repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalonService>>> GetAll(CancellationToken cancellationToken)
        => Ok(await repository.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<SalonService>> GetById(string id, CancellationToken cancellationToken)
    {
        var service = await repository.GetAsync(id, cancellationToken);
        return service is null ? NotFound() : Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<SalonService>> Create(SalonServiceRequest request, CancellationToken cancellationToken)
    {
        var service = Apply(new SalonService(), request);
        var created = await repository.CreateAsync(service, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SalonService>> Update(string id, SalonServiceRequest request, CancellationToken cancellationToken)
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

    private static SalonService Apply(SalonService service, SalonServiceRequest request)
    {
        service.Name = request.Name;
        service.Description = request.Description;
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.IsAvailable = request.IsAvailable;
        return service;
    }
}
