using System.ComponentModel.DataAnnotations;

namespace CatalogService.Entities
{
    public class Event
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        [Required]
        public string Venue { get; set; } = string.Empty;

        [Range(0, 1000000)]
        public decimal Price { get; set; }

        [Range(0, 100000)]
        public int AvailableTickets { get; set; }
    }
}