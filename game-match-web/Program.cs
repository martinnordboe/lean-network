using game_match.Components;
using game_match.Repositories;
using game_match.Services;
using game_match.tools;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

string? connectionString = DotEnv.ReadMySqlConnectionString();
builder.Services.AddSingleton(new DbConnectionFactory(connectionString));
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddHttpClient("Activities", http =>
{
	var url = builder.Configuration["ActivitiesApi:BaseUrl"]
	?? throw new InvalidOperationException("ActivitiesApi:BaseUrl mangler.");
	http.BaseAddress = new Uri(url);
	http.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<ActivityApiClient>();

IDataProtectionBuilder dataProtection = builder.Services.AddDataProtection().SetApplicationName("GameMatch.Session");
string? keyRingPath = builder.Configuration["Authentication:SharedKeyRingPath"];
if (!string.IsNullOrWhiteSpace(keyRingPath))
{
	dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
}

CookieSecurePolicy securePolicy = CookieSecurePolicy.Always;
if (builder.Environment.IsDevelopment())
{
	securePolicy = CookieSecurePolicy.SameAsRequest;
}

builder.Services.AddAntiforgery(options =>
{
	options.Cookie.Name = ActivityApiClient.AntiforgeryCookieName;
	options.Cookie.SecurePolicy = securePolicy;
});

builder.Services.AddHttpClient("Activities", http =>
{
	string url = builder.Configuration["ActivitiesApi:BaseUrl"]
	?? throw new InvalidOperationException("ActivitiesApi:BaseUrl mangler.");
	http.BaseAddress = new Uri(url);
	http.Timeout = TimeSpan.FromSeconds(5);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
	// Send kun cookies fra den aktuelle bruger på det enkelte API-kald.
	UseCookies = false,
	AllowAutoRedirect = false
});
builder.Services.AddScoped<ActivityApiClient>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
	options.Cookie.Name = ActivityApiClient.SessionCookieName;
	options.Cookie.SecurePolicy = securePolicy;
	options.SlidingExpiration = false;
	options.LoginPath = "/loginForm";
	options.AccessDeniedPath = "/Error";
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	using IServiceScope scope = app.Services.CreateScope();
	await scope.ServiceProvider.GetRequiredService<UserService>().EnsureAdminIsAddedAsync();
}

static void AddLogoutEndpoint(WebApplication app)
{
	app.MapPost("/account/login", async (HttpContext context, IAntiforgery antiforgery, ActivityApiClient activityApi) =>
	{
		try
		{
			await antiforgery.ValidateRequestAsync(context);
		}
		catch (AntiforgeryValidationException)
		{
			return Results.BadRequest("Formularen er udløbet. Genindlæs login-siden og prøv igen.");
		}

		try
		{
			bool signedIn = await activityApi.LoginAsync(context);
			if (signedIn)
			{
				return Results.LocalRedirect("/account");
			}

			return Results.LocalRedirect("/loginForm?error=invalid");
		}
		catch (Exception ex) when (ex is HttpRequestException ||
								   ex is OperationCanceledException && !context.RequestAborted.IsCancellationRequested)
		{
			return Results.LocalRedirect("/loginForm?error=unavailable");
		}
	}).AllowAnonymous();

	app.MapPost("/account/logout", async (HttpContext context, IAntiforgery antiforgery) =>
	{
		try
		{
			await antiforgery.ValidateRequestAsync(context);
		}
		catch (AntiforgeryValidationException)
		{
			return Results.BadRequest("Formularen er udløbet. Genindlæs siden og prøv igen.");
		}

		await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

		return Results.Redirect("/loginForm");
	}).RequireAuthorization();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

AddLogoutEndpoint(app);

RouteGroupBuilder activityActions = app.MapGroup("/activities/{id:int}").RequireAuthorization();
activityActions.AddEndpointFilter(ValidateActivityRequest);
activityActions.MapPost("/delete", DeleteActivityAsync);
activityActions.MapPost("/edit", EditActivityAsync);

app.MapPost("/activities/create", CreateActivityAsync)
	.RequireAuthorization()
	.AddEndpointFilter(ValidateActivityRequest);

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();

static async ValueTask<object?> ValidateActivityRequest(
	EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
{
	HttpContext context = invocation.HttpContext;
	IAntiforgery antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
	try
	{
		await antiforgery.ValidateRequestAsync(context);
		return await next(invocation);
	}
	catch (AntiforgeryValidationException)
	{
		return Results.BadRequest("Formularen er udløbet. Genindlæs siden og prøv igen.");
	}
	catch (HttpRequestException)
	{
		return Results.StatusCode(StatusCodes.Status502BadGateway);
	}
	catch (OperationCanceledException)
	{
		if (context.RequestAborted.IsCancellationRequested)
		{
			throw;
		}

		return Results.StatusCode(StatusCodes.Status502BadGateway);
	}
}

static async Task<IResult> CreateActivityAsync(
	game_match.Models.Activity activity, HttpContext context, ActivityApiClient activityApi)
{
	using HttpResponseMessage response = await activityApi.AddAsync(activity, context);
	return Results.StatusCode((int)response.StatusCode);
}

static async Task<IResult> DeleteActivityAsync(int id, HttpContext context, ActivityApiClient activityApi)
{
	using HttpResponseMessage response = await activityApi.DeleteByIdAsync(id, context);
	return Results.StatusCode((int)response.StatusCode);
}

static async Task<IResult> EditActivityAsync(
	int id, game_match.Models.Activity activity, HttpContext context, ActivityApiClient activityApi)
{
	using HttpResponseMessage response = await activityApi.EditByIdAsync(id, activity, context);
	return Results.StatusCode((int)response.StatusCode);
}
