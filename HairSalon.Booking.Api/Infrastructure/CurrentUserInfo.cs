namespace HairSalon.Booking.Api.Infrastructure;

public sealed record CurrentUserInfo(
    string Provider,
    string ProviderUserId,
    string Name,
    string Email)
{
    public string LocalUserId => $"{Provider}-{ProviderUserId}".Replace("|", "-", StringComparison.Ordinal);
}
