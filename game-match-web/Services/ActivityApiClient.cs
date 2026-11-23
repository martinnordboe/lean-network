using game_match.Models;
using System.Text.Json;
namespace game_match.Services
{
    public class ActivityApiClient(IHttpClientFactory factory)
    {
        public async Task<List<Activity>> GetAllAsync()
        {
            using var http = factory.CreateClient("Activities");
            using var response = await http.GetAsync("api/activities");
            response.EnsureSuccessStatusCode();

            var activities = await response.Content.ReadFromJsonAsync<List<Activity>>();
            return activities ?? throw new JsonException("Api returnerede null.");
        }

        public async Task<Activity> GetByIdAsync(int id)
        {
            using HttpClient http = factory.CreateClient("Activities");
            using HttpResponseMessage response = await http.GetAsync($"api/activities/{id}");
            response.EnsureSuccessStatusCode();

            Activity? activity = await response.Content.ReadFromJsonAsync<Activity>();
            return activity ?? throw new JsonException("Api returnerede null");
        }
    }
}
