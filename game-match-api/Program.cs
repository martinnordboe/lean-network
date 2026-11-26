using System.Globalization;
using System.Security.Claims;
using game_match_api.Models;
using game_match_api.Repositories;
using game_match_api.Services;
using game_match_api.tools;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string? connectionString = DotEnv.ReadMySqlConnectionString();
builder.Services.AddSingleton(new DbConnectionFactory(connectionString));
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddValidation(); //Gør det samme som BookRules fra underivsningen gjorde. Tjekker [Required] osv på Activity automatisk.
builder.Services.AddOpenApi();


// I Compose skal vi have Authentication__SharedKeyRingPath til at pege på nøglemappen i begge containere.
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

// API validerer Blazors CSRF token fra CSRF/Antiforgery token
builder.Services.AddAntiforgery(options =>
{
	options.Cookie.Name = ".GameMatch.Antiforgery";
	options.Cookie.SecurePolicy = securePolicy;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
	options.Cookie.Name = ".GameMatch.Session";
	options.Cookie.SecurePolicy = securePolicy;
	options.SlidingExpiration = false;

	options.Events.OnRedirectToLogin = context =>
	{
		context.Response.StatusCode = StatusCodes.Status401Unauthorized;
		return Task.CompletedTask;
	};
	options.Events.OnRedirectToAccessDenied = context =>
	{
		context.Response.StatusCode = StatusCodes.Status403Forbidden;
		return Task.CompletedTask;
	};
});
builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();


//Endpoints
#region ENDPOINTS
// LOgin endpointt
app.MapPost("/api/account/login", async (HttpContext context, UserService userService) =>
{
	IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
	string username = form["username"].ToString();
	string password = form["password"].ToString();
	if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
	{
		return Results.Unauthorized();
	}

	User? user = await userService.ValidateCredentials(username, password);
	if (user == null)
	{
		return Results.Unauthorized();
	}

	Claim[] claims = new Claim[]
	{
		new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
		new Claim(ClaimTypes.Name, user.Username),
		new Claim(ClaimTypes.Role, user.Role)
	};
	ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
	await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false });
	return Results.Ok();
}).AllowAnonymous().AddEndpointFilter(ValidateAntiforgery);

// LOGROUT ENDPOINT
app.MapPost("/api/account/logout", async (HttpContext context) =>
{
	await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

	return Results.Ok();
	// KRÆVER HTTPONLY COOKIE AUTH FOR AT KUNNE LOGGE UD, SAMT CSRF (ANTIFORGERY) TOKEN I FORMULAR
}).RequireAuthorization().AddEndpointFilter(ValidateAntiforgery);

app.MapGet("/api/activities", async (IActivityService activityService) =>
{
	List<Activity> activities = await activityService.GetAllActivitiesAsync();
	return Results.Ok(activities);
});

app.MapGet("/api/activities/{id:int}", async (int id, IActivityService activityService) =>
{
	try
	{
		Activity activity = await activityService.GetActivityById(id);
		return Results.Ok(activity);
	}
	catch (Exception ex)
	{
		if (ex.Message == "Activity not found")
		{
			return Results.NotFound();
		}
		throw;
	}
});

app.MapDelete("/api/activities/{id:int}", async (int id, IActivityService activityService) =>
	{
		try
		{
			await activityService.RemoveActivityAsync(id);
			return Results.NoContent();
		}
		catch (Exception ex)
		{
			if (ex.Message == "Activity not found")
			{
				return Results.NotFound();
			}
			throw;
		}


	});

app.MapPut("/api/activities/{id:int}", async (int id, Activity activity, IActivityService activityService) =>
{

	try
	{
		activity.Id = id;
		await activityService.UpdateToRepoAsync(activity);
		return Results.NoContent();
	}
	catch (Exception ex)
	{
		if (ex.Message == "Activity not found")
		{
			return Results.NotFound();
		}
		throw;
	}

});

app.MapPost("api/activities", async (Activity input, IActivityService activityService) =>
{
	await activityService.AddToRepoAsync(input);
	return Results.Created($"/api/activities/{input.Id}", input); //201
});
#endregion

static async ValueTask<object?> ValidateAntiforgery(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
{
	HttpContext context = invocation.HttpContext;
	IAntiforgery antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
	try
	{
		await antiforgery.ValidateRequestAsync(context);
	}
	catch (AntiforgeryValidationException)
	{
		return Results.BadRequest("Request mangler gyldigt CSRF");
	}

	return await next(invocation);
}

static bool IsOwner(ClaimsPrincipal user, Activity activity)
{
	return user.FindFirstValue(ClaimTypes.NameIdentifier) == activity.OwnerId.ToString(CultureInfo.InvariantCulture);
}

app.Run();
