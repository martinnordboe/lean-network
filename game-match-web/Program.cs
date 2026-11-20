using game_match.Components;
using game_match.Models;
using game_match.Repositories;
using game_match.Services;
using game_match.tools;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;


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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
    {
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
    app.MapPost("/account/login", async (HttpContext context, IAntiforgery antiforgery, UserService userService) =>
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest("Formularen er udløbet. Genindlæs login-siden og prøv igen.");
        }

        IFormCollection form = await context.Request.ReadFormAsync();
        User? user = await userService.ValidateCredentials(form["username"].ToString(), form["password"].ToString());
        if (user == null)
        {
            return Results.LocalRedirect("/loginForm?error=invalid");
        }

        Claim[] claims = new Claim[]
        {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
				new Claim(ClaimTypes.Role, user.Role)
		};
        ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false });
        return Results.LocalRedirect("/account");
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
    });
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
