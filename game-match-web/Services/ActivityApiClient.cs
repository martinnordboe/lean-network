using System.Net;
using System.Text.Json;
using game_match.Models;
using Microsoft.Net.Http.Headers;
namespace game_match.Services
{
	public class ActivityApiClient(IHttpClientFactory factory)
	{
		internal const string SessionCookieName = ".GameMatch.Session";
		internal const string AntiforgeryCookieName = ".GameMatch.Antiforgery";
		private const string AntiforgeryHeaderName = "RequestVerificationToken";
		private const string AntiforgeryFormFieldName = "__RequestVerificationToken";

		public async Task<bool> LoginAsync(HttpContext context)
		{
			IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "api/account/login")
			{
				Content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["username"] = form["username"].ToString(),
					["password"] = form["password"].ToString()
				})
			};
			using HttpResponseMessage response = await SendAsync(request, context, sendCsrf: true);
			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				return false;
			}
			response.EnsureSuccessStatusCode();

			if (!response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? headers))
			{
				throw new HttpRequestException("API'et returnerede ingen sessionscookie.");
			}

			SetCookieHeaderValue sessionCookie = SetCookieHeaderValue.ParseList(headers.ToArray())
				.FirstOrDefault(cookie => cookie.Name.Value == SessionCookieName)
				?? throw new HttpRequestException("API'et returnerede ingen sessionscookie.");
			context.Response.Headers.Append(HeaderNames.SetCookie, sessionCookie.ToString());
			return true;
		}

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

		public async Task DeleteByIdAsync(int id)
		{
			using HttpClient http = factory.CreateClient("Activities");
			using HttpResponseMessage response = await http.DeleteAsync($"api/activities/{id}");
			response.EnsureSuccessStatusCode();
		}

		public async Task EditByIdAsync(int id, Activity activity)
		{
			using HttpClient http = factory.CreateClient("Activities");
			using HttpResponseMessage response = await http.PutAsJsonAsync($"api/activities/{id}", activity);
			response.EnsureSuccessStatusCode();
		}

		public async Task AddAsync(Activity activity)
		{
			using HttpClient http = factory.CreateClient("Activities");
			using HttpResponseMessage response = await http.PostAsJsonAsync("api/activities", activity);
			response.EnsureSuccessStatusCode();
		}

		private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpContext context, bool sendCsrf = false)
		{
			List<string> cookies = new List<string>();
			if (context.Request.Cookies.TryGetValue(SessionCookieName, out string? sessionCookie))
			{
				cookies.Add($"{SessionCookieName}={sessionCookie}");
			}

			// login, redigering og sletning genbruger API Blazors CSRF token.
			if (sendCsrf && context.Request.Cookies.TryGetValue(AntiforgeryCookieName, out string? antiforgeryCookie))
			{
				cookies.Add($"{AntiforgeryCookieName}={antiforgeryCookie}");
				string? token = context.Request.Headers[AntiforgeryHeaderName];
				if (string.IsNullOrEmpty(token) && context.Request.HasFormContentType)
				{
					IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
					token = form[AntiforgeryFormFieldName];
				}

				if (!string.IsNullOrEmpty(token))
				{
					request.Headers.Add(AntiforgeryHeaderName, token);
				}
			}

			if (cookies.Count > 0)
			{
				request.Headers.Add(HeaderNames.Cookie, string.Join("; ", cookies));
			}

			using HttpClient http = factory.CreateClient("Activities");
			return await http.SendAsync(request, context.RequestAborted);
		}
	}
}
