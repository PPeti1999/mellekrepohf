using BookingService.Models;
using System.Text.Json; // <--- FONTOS: Ez kell a JsonSerializerOptions-hoz

namespace BookingService.Services
{
    public interface ICatalogClient
    {
        Task<CatalogEventDto?> GetEventAsync(Guid eventId);
    }

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
                // JAVÍTÁS: Megmondjuk a JSON olvasónak, hogy ne törődjön a kis/nagybetűkkel!
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true 
                };

                // Átadjuk az options-t a hívásnak
                var eventData = await _httpClient.GetFromJsonAsync<CatalogEventDto>($"/api/Events/{eventId}", options);
                
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nem sikerült elérni a Catalog Service-t az {EventId} lekérdezésekor.", eventId);
                return null;
            }
        }
    }
}