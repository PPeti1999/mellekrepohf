using BookingService.Models;

namespace BookingService.Services
{
    // Interfész, hogy a Controller csak ezt ismerje
    public interface ICatalogClient
    {
        Task<CatalogEventDto?> GetEventAsync(Guid eventId);
    }

    // A konkrét megvalósítás
    public class CatalogClient : ICatalogClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CatalogClient> _logger;

        public CatalogClient(HttpClient httpClient, ILogger<CatalogClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CatalogEventDto?> GetEventAsync(Guid eventId)
        {
            try
            {
                // Itt hívjuk meg a Catalog Service-t. 
                // A BaseAddress-t a Program.cs-ben állítjuk be, itt csak az útvonal kell.
                var eventData = await _httpClient.GetFromJsonAsync<CatalogEventDto>($"/api/Events/{eventId}");
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nem sikerült elérni a Catalog Service-t az {EventId} lekérdezésekor.", eventId);
                return null; // Ha hiba van, null-t adunk vissza
            }
        }
    }
}