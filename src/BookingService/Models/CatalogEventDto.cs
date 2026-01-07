using System.Text.Json.Serialization;

namespace BookingService.Models
{
    public class CatalogEventDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AvailableTickets { get; set; }
    }
}