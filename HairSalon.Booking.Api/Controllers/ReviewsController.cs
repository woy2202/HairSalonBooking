using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Api.Model;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Booking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ReviewsController : ControllerBase
    {
        private const int MaximumPublicReviews = 20;
        private const int MaximumContentLength = 1000;

        private readonly IBookingRepository<Review> _repository;
        private readonly IBookingRepository<Customer> _customers;
        private readonly IBookingRepository<Appointment> _appointments;
        private readonly IBookingRepository<Hairdresser> _hairdressers;
        private readonly IBookingRepository<SalonService> _services;
        private readonly ICurrentUserService _currentUser;
        private readonly IAppointmentStatusService _appointmentStatusService;

        public ReviewsController(
            IBookingRepository<Review> repository,
            IBookingRepository<Customer> customers,
            IBookingRepository<Appointment> appointments,
            IBookingRepository<Hairdresser> hairdressers,
            IBookingRepository<SalonService> services,
            ICurrentUserService currentUser,
            IAppointmentStatusService appointmentStatusService)
        {
            _repository = repository;
            _customers = customers;
            _appointments = appointments;
            _hairdressers = hairdressers;
            _services = services;
            _currentUser = currentUser;
            _appointmentStatusService = appointmentStatusService;
        }

        [HttpGet("public")]
        public async Task<ActionResult<IReadOnlyList<Review>>> GetPublic(CancellationToken cancellationToken)
        {
            var reviews = await _repository.GetAllAsync(cancellationToken);
            return Ok(reviews
                .Where(review => review.IsVisible)
                .OrderByDescending(review => review.CreatedAt)
                .Take(MaximumPublicReviews)
                .ToList());
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Review>>> GetAll(CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może wyświetlić wszystkie opinie." });
            }

            var reviews = await _repository.GetAllAsync(cancellationToken);
            return Ok(reviews.OrderByDescending(review => review.CreatedAt).ToList());
        }

        [HttpGet("me")]
        public async Task<ActionResult<IReadOnlyList<Review>>> GetMine(CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.CustomerId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Zalogowany użytkownik nie ma przypisanego profilu klienta." });
            }

            var reviews = await _repository.GetAllAsync(cancellationToken);
            return Ok(reviews
                .Where(review => review.CustomerId == user.CustomerId)
                .OrderByDescending(review => review.CreatedAt)
                .ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetById(string id, CancellationToken cancellationToken)
        {
            var review = await _repository.GetAsync(id, cancellationToken);
            if (review is null)
            {
                return NotFound(new { error = "Nie znaleziono opinii." });
            }

            if (review.IsVisible || await _currentUser.IsAdminAsync(cancellationToken))
            {
                return Ok(review);
            }

            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            return user?.CustomerId == review.CustomerId
                ? Ok(review)
                : StatusCode(StatusCodes.Status403Forbidden, new { error = "Nie masz uprawnień do odczytu tej opinii." });
        }

        [HttpPost]
        public async Task<ActionResult<Review>> Create(ReviewRequest request, CancellationToken cancellationToken)
        {
            var user = await _currentUser.GetCurrentAppUserAsync(cancellationToken);
            if (user?.CustomerId is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko klient może dodać opinię." });
            }

            var customer = await _customers.GetAsync(user.CustomerId, cancellationToken);
            if (customer is null)
            {
                return NotFound(new { error = "Nie znaleziono profilu klienta przypisanego do użytkownika." });
            }

            var validationError = await ValidateRequestAsync(user.CustomerId, request, cancellationToken);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return BadRequest(new { error = validationError });
            }

            var review = await BuildReviewAsync(customer, request, cancellationToken);
            var created = await _repository.CreateAsync(review, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
        }

        [HttpPatch("{id}/hide")]
        public async Task<ActionResult<Review>> Hide(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może ukrywać opinie." });
            }

            var review = await _repository.GetAsync(id, cancellationToken);
            if (review is null)
            {
                return NotFound(new { error = "Nie znaleziono opinii do ukrycia." });
            }

            review.IsVisible = false;
            return Ok(await _repository.UpsertAsync(review, cancellationToken));
        }

        [HttpPatch("{id}/show")]
        public async Task<ActionResult<Review>> Show(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może ponownie pokazywać opinie." });
            }

            var review = await _repository.GetAsync(id, cancellationToken);
            if (review is null)
            {
                return NotFound(new { error = "Nie znaleziono opinii do pokazania." });
            }

            review.IsVisible = true;
            return Ok(await _repository.UpsertAsync(review, cancellationToken));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            if (!await _currentUser.IsAdminAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Tylko administrator może usuwać opinie." });
            }

            var review = await _repository.GetAsync(id, cancellationToken);
            if (review is null)
            {
                return NotFound(new { error = "Nie znaleziono opinii do usunięcia." });
            }

            await _repository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        private async Task<string?> ValidateRequestAsync(string customerId, ReviewRequest request, CancellationToken cancellationToken)
        {
            if (request.Rating < 1 || request.Rating > 5)
            {
                return "Ocena musi być liczbą od 1 do 5.";
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return "Treść opinii jest wymagana.";
            }

            if (request.Content.Length > MaximumContentLength)
            {
                return $"Treść opinii może mieć maksymalnie {MaximumContentLength} znaków.";
            }

            if (!string.IsNullOrWhiteSpace(request.AppointmentId))
            {
                return await ValidateAppointmentReviewAsync(customerId, request.AppointmentId, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(request.HairdresserId) &&
                await _hairdressers.GetAsync(request.HairdresserId, cancellationToken) is null)
            {
                return "Nie znaleziono fryzjera przypisanego do opinii.";
            }

            if (!string.IsNullOrWhiteSpace(request.SalonServiceId) &&
                await _services.GetAsync(request.SalonServiceId, cancellationToken) is null)
            {
                return "Nie znaleziono usługi przypisanej do opinii.";
            }

            return null;
        }

        private async Task<string?> ValidateAppointmentReviewAsync(string customerId, string appointmentId, CancellationToken cancellationToken)
        {
            var appointment = await _appointments.GetAsync(appointmentId, cancellationToken);
            if (appointment is null)
            {
                return "Nie znaleziono wizyty przypisanej do opinii.";
            }

            appointment = await _appointmentStatusService.RefreshExpiredAppointmentAsync(appointment, cancellationToken);
            if (appointment.CustomerId != customerId)
            {
                return "Klient może dodać opinię tylko do swojej wizyty.";
            }

            if (appointment.Status != AppointmentStatus.Completed)
            {
                return "Opinię do wizyty można dodać dopiero po zakończeniu wizyty.";
            }

            var reviews = await _repository.GetAllAsync(cancellationToken);
            return reviews.Any(review => review.CustomerId == customerId && review.AppointmentId == appointmentId)
                ? "Do tej wizyty została już dodana opinia."
                : null;
        }

        private async Task<Review> BuildReviewAsync(Customer customer, ReviewRequest request, CancellationToken cancellationToken)
        {
            var review = new Review
            {
                CustomerId = customer.id,
                AppointmentId = EmptyToNull(request.AppointmentId),
                HairdresserId = EmptyToNull(request.HairdresserId),
                SalonServiceId = EmptyToNull(request.SalonServiceId),
                DisplayName = BuildDisplayName(customer),
                Rating = request.Rating,
                Content = request.Content.Trim(),
                IsVisible = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(request.AppointmentId))
            {
                var appointment = await _appointments.GetAsync(request.AppointmentId, cancellationToken);
                if (appointment is not null)
                {
                    review.HairdresserId = appointment.HairdresserId;
                    review.SalonServiceId = appointment.SalonServiceId;
                }
            }

            return review;
        }

        private static string BuildDisplayName(Customer customer)
        {
            var name = string.Join(" ", new[] { customer.FirstName, customer.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            return string.IsNullOrWhiteSpace(name) ? "Klient salonu" : name;
        }

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
