namespace HairSalon.Booking.Functions.Options
{
    public sealed class CosmosOptions
    {
        public string? ConnectionString { get; set; }
        public string DatabaseName { get; set; } = "BookingApkaDB";
        public string CustomersContainerName { get; set; } = "customers";
        public string HairdressersContainerName { get; set; } = "barbers";
        public string SalonServicesContainerName { get; set; } = "services";
        public string AppointmentsContainerName { get; set; } = "appointments";
    }
}
