using EduLearn.SharedLib.Extensions;
using EduLearn.AuthService.Data;
using EduLearn.AuthService.Models;
using EduLearn.AuthService.Repositories;
using EduLearn.AuthService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EduLearn.SharedLib.Middlewares;
using EduLearn.AuthService.Mappings;

var builder = WebApplication.CreateBuilder(args);

// Database & Core Services
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Authentication
builder.Services.AddSharedJwtAuthentication(builder.Configuration)
    .AddCookie(options => 
    {
        options.Cookie.Name = "EduLearn.ExternalAuth";
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.SignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    });

// Error Handling & API
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddSharedSwagger("EduLearn Auth Service");
builder.Services.AddAzureStorage();
builder.Services.AddSharedMessaging(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")  // Your Angular/React URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Database Migration & Seeding at Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<UserDbContext>();
        Console.WriteLine("[STARTUP] Applying Database Migrations...");
        await context.Database.MigrateAsync();

        Console.WriteLine("[STARTUP] Seeding Administrative Data...");
        await DbSeeder.SeedAdminAsync(services, app.Configuration);

        Console.WriteLine("[STARTUP] AuthService is ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Startup failed: {ex.Message}");
    }
}

app.Run();
