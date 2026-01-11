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

        public CatalogClient(IHttpClientFactory factory, ILogger<CatalogClient> logger)
        {
            _httpClient = factory.CreateClient("catalog");
            _logger = logger;
        }

        public async Task<CatalogEventDto?> GetEventAsync(Guid id)
        {
            try
            {
                // Beállítjuk, hogy ne legyen érzékeny a kis/nagybetűkre (camelCase vs PascalCase)
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Lekérjük az adatot
                var response = await _httpClient.GetAsync($"api/Events/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[CatalogClient] Hiba a lekéréskor. Status: {response.StatusCode}, URL: {response.RequestMessage?.RequestUri}");
                    return null;
                }

                var eventData = await response.Content.ReadFromJsonAsync<CatalogEventDto>(options);
                
                if (eventData == null)
                {
                    _logger.LogWarning($"[CatalogClient] A válasz JSON üres volt. ID: {id}");
                }

                return eventData;
            }
            catch (Exception ex)
            {
                // Ez a log megjelenik majd a 'docker logs booking-service' parancsnál
                _logger.LogError(ex, $"[CatalogClient] KIVÉTEL történt a CatalogService hívásakor! ID: {id}");
                return null;
            }
        }
    }
}