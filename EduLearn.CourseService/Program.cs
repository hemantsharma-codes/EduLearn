using EduLearn.SharedLib.Extensions;
using EduLearn.CourseService.Data;
using EduLearn.CourseService.Mappings;
using EduLearn.CourseService.Repositories;
using EduLearn.CourseService.Services;
using Microsoft.EntityFrameworkCore;
using EduLearn.SharedLib.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Core Services
builder.Services.AddControllers();
builder.Services.AddSharedSwagger("EduLearn Course Service");
builder.Services.AddAzureStorage();
builder.Services.AddSharedMessaging(builder.Configuration, x => 
{
    x.AddConsumer<EduLearn.CourseService.Consumers.CourseUserDeletedConsumer>();
    x.AddConsumer<EduLearn.CourseService.Consumers.CourseEnrollmentCreatedConsumer>();
    x.AddConsumer<EduLearn.CourseService.Consumers.CourseEnrollmentDroppedConsumer>();
});

// Database
builder.Services.AddDbContext<CourseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DI
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, EduLearn.CourseService.Services.CourseService>();
builder.Services.AddAutoMapper(cfg => {}, typeof(AutoMapperProfile));

// Error Handling & Auth
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database Migration at Startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
        Console.WriteLine("[STARTUP] Applying Course Database Migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("[STARTUP] CourseService is ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] CourseService Startup failed: {ex.Message}");
    }
}

app.Run();
