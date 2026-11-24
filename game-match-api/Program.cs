using game_match_api.DTO;
using game_match_api.Models;
using game_match_api.Repositories;
using game_match_api.Services;
using game_match_api.tools;

using Microsoft.AspNetCore.Authentication.BearerToken;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string? connectionString = DotEnv.ReadMySqlConnectionString();
builder.Services.AddSingleton(new DbConnectionFactory(connectionString));
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
	.AddBearerToken(options =>
	{
		options.BearerTokenExpiration = TimeSpan.FromMinutes(15);
	});
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();

//Endpoints
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

app.MapPost("/auth/login", async Task<IResult> (LoginInput input, UserService userService) =>
{
	var principal = await userService.Authenticate(input.Username, input.Password);
	return principal is null
		? Results.Problem(statusCode: 401, title: "Brugernavn eller adgangskode er forkert.")
		: Results.SignIn(principal, authenticationScheme: BearerTokenDefaults.AuthenticationScheme);
});

app.Run();
