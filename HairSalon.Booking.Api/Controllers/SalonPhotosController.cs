using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Options;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SalonPhotosController(
    IBookingRepository<SalonPhoto> repository,
    IPhotoStorageService photoStorage,
    IOptions<AzureBookingOptions> options) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalonPhoto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await repository.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<SalonPhoto>> GetById(string id, CancellationToken cancellationToken)
    {
        var photo = await repository.GetAsync(id, cancellationToken);
        return photo is null ? NotFound() : Ok(photo);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<SalonPhoto>> Upload(IFormFile file, [FromForm] string? caption, CancellationToken cancellationToken)
    {
        try
        {
            var uploaded = await photoStorage.UploadAsync(options.Value.Storage.SalonPhotoBlobContainer, file, cancellationToken);
            var photo = new SalonPhoto
            {
                FileName = uploaded.FileName,
                BlobName = uploaded.BlobName,
                BlobUrl = uploaded.Url,
                Caption = caption
            };

            var created = await repository.CreateAsync(photo, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var photo = await repository.GetAsync(id, cancellationToken);
        if (photo is null)
        {
            return NotFound();
        }

        await photoStorage.DeleteAsync(options.Value.Storage.SalonPhotoBlobContainer, photo.BlobName, cancellationToken);
        await repository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
