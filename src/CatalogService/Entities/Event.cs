using System;

namespace CatalogService.Entities
{
    public class Event
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Ezek hiányoztak a Controllerből:
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int AvailableTickets { get; set; }

        public DateTime Date { get; set; }
        public decimal Price { get; set; }
    }
}