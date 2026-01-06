using System;

namespace BookingService.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        
        // Ez a mező hiányzott, ezért volt a hiba:
        public string EventName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;
        public int TicketCount { get; set; }
        public DateTime BookingDate { get; set; }
    }
}