using System.Collections;

namespace HairSalon.Booking.Core.Patterns
{
    public sealed class AvailabilitySlotCollection : IEnumerable<DateTimeOffset>
    {
        private readonly List<DateTimeOffset> _slots = new List<DateTimeOffset>();

        public AvailabilitySlotCollection(DateTimeOffset day, TimeOnly opensAt, TimeOnly closesAt, int stepMinutes)
        {
            var current = new DateTimeOffset(day.Date + opensAt.ToTimeSpan(), day.Offset);
            var end = new DateTimeOffset(day.Date + closesAt.ToTimeSpan(), day.Offset);

            while (current < end)
            {
                _slots.Add(current);
                current = current.AddMinutes(stepMinutes);
            }
        }

        public IEnumerator<DateTimeOffset> GetEnumerator()
        {
            return _slots.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
