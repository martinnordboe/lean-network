using game_match_api.Models;
using game_match_api.Repositories;
using game_match_api.Services;
using game_match_api.tools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string? connectionString = DotEnv.ReadMySqlConnectionString();
builder.Services.AddSingleton(new DbConnectionFactory(connectionString));
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


//Endpoints
app.MapGet("/api/activities", async (IActivityService activityService) =>
{
    List<Activity> activities = await activityService.GetAllActivitiesAsync();
    return Results.Ok(activities);
});
app.MapPost("/api/activities", async (IActivityService service) =>
{
    service.AddToRepoAsync()
});
app.Run();
