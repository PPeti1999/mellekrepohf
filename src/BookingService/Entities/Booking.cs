using System.ComponentModel.DataAnnotations;

namespace BookingService.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int TicketCount { get; set; }
        public DateTime BookingDate { get; set; }
    }
}