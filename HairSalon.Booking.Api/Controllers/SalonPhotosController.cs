using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class SalonPhotosController : ControllerBase
    {
        private readonly IBookingRepository<SalonPhoto> _repository;
        private readonly IPhotoStorageService _photoStorage;
        private readonly ICurrentUserService _currentUser;
        private readonly IOptions<AzureBookingOptions> _options;

        public SalonPhotosController(
            IBookingRepository<SalonPhoto> repository,
            IPhotoStorageService photoStorage,
            ICurrentUserService currentUser,
            IOptions<AzureBookingOptions> options)
        {
            _repository = repository;
            _photoStorage = photoStorage;
            _currentUser = currentUser;
            _options = options;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SalonPhoto>>> GetAll(CancellationToken cancellationToken)
        {
            var photos = await _repository.GetAllAsync(cancellationToken);
            return Ok(photos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalonPhoto>> GetById(string id, CancellationToken cancellationToken)
        {
            var photo = await _repository.GetAsync(id, cancellationToken);
            return photo is null ? NotFound(new { error = "Nie znaleziono zdjêcia salonu." }) : Ok(photo);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SalonPhoto>> Upload(IFormFile file, [FromForm] string? caption, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator mo¿e dodawaæ zdjêcia salonu." });
            }

            try
            {
                var uploaded = await _photoStorage.UploadAsync(_options.Value.Storage.SalonPhotoBlobContainer, file, cancellationToken);
                var photo = new SalonPhoto
                {
                    FileName = uploaded.FileName,
                    BlobName = uploaded.BlobName,
                    BlobUrl = uploaded.Url,
                    Caption = caption,
                    DisplayWidth = uploaded.DisplayWidth,
                    DisplayHeight = uploaded.DisplayHeight
                };

                var created = await _repository.CreateAsync(photo, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SalonPhoto>> Update(string id, SalonPhotoRequest request, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator mo¿e edytowaæ opis zdjêcia salonu." });
            }

            var existing = await _repository.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { error = "Nie znaleziono zdjêcia salonu do edycji." });
            }

            existing.Caption = request.Caption;
            var updated = await _repository.UpsertAsync(existing, cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator mo¿e usuwaæ zdjêcia salonu." });
            }

            var photo = await _repository.GetAsync(id, cancellationToken);
            if (photo is null)
            {
                return NotFound(new { error = "Nie znaleziono zdjêcia salonu do usuniêcia." });
            }

            await _photoStorage.DeleteAsync(_options.Value.Storage.SalonPhotoBlobContainer, photo.BlobName, cancellationToken);
            await _repository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
