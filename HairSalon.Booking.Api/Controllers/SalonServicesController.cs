using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class SalonServicesController : ControllerBase
    {
        private readonly IBookingRepository<SalonService> _repository;
        private readonly IBookingRepository<Appointment> _appointments;
        private readonly ICurrentUserService _currentUser;

        public SalonServicesController(
            IBookingRepository<SalonService> repository,
            IBookingRepository<Appointment> appointments,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _appointments = appointments;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SalonService>>> GetAll(CancellationToken cancellationToken)
        {
            var services = await _repository.GetAllAsync(cancellationToken);
            return Ok(services);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalonService>> GetById(string id, CancellationToken cancellationToken)
        {
            var service = await _repository.GetAsync(id, cancellationToken);
            return service is null ? NotFound(new { error = "Nie znaleziono usługi salonu." }) : Ok(service);
        }

        [HttpPost]
        public async Task<ActionResult<SalonService>> Create(SalonServiceRequest request, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może dodawać usługi salonu." });
            }

            var service = Apply(new SalonService(), request);
            var created = await _repository.CreateAsync(service, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SalonService>> Update(string id, SalonServiceRequest request, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może edytować usługi salonu." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono usługi do edycji." });
            }

            var updated = await _repository.UpsertAsync(Apply(existing, request), cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może usuwać usługi salonu." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono usługi do usunięcia." });
            }

            if (await HasAppointmentsAsync(id, cancellationToken))
            {
                return BadRequest(new { error = "Nie można usunąć usługi, ponieważ jest przypisana do wizyt. Najpierw anuluj albo usuń powiązane wizyty." });
            }

            await _repository.DeleteAsync(id, cancellationToken);
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

        private async Task<bool> HasAppointmentsAsync(string serviceId, CancellationToken cancellationToken)
        {
            var appointments = await _appointments.GetAllAsync(cancellationToken);
            return appointments.Any(appointment => appointment.SalonServiceId == serviceId);
        }
    }
}
