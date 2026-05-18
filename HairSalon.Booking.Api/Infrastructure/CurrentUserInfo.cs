namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class CurrentUserInfo
    {
        public CurrentUserInfo(string provider, string providerUserId, string name, string email)
        {
            Provider = provider;
            ProviderUserId = providerUserId;
            Name = name;
            Email = email;
        }

        public string Provider { get; }

        public string ProviderUserId { get; }

        public string Name { get; }

        public string Email { get; }

        public string LocalUserId
        {
            get
            {
                return $"{Provider}-{ProviderUserId}".Replace("|", "-", StringComparison.Ordinal);
            }
        }
    }
}
