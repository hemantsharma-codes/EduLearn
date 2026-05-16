using EduLearn.SharedLib.Extensions;
using EduLearn.EnrollmentService.Data;
using EduLearn.EnrollmentService.Mappings;
using EduLearn.EnrollmentService.Repositories;
using EduLearn.EnrollmentService.Services;
using Microsoft.EntityFrameworkCore;
using EduLearn.SharedLib.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Core Services
builder.Services.AddControllers();
builder.Services.AddSharedSwagger("EduLearn Enrollment Service");
builder.Services.AddSharedMessaging(builder.Configuration, x => 
{
    x.AddConsumer<EduLearn.EnrollmentService.Consumers.EnrollmentUserDeletedConsumer>();
    x.AddConsumer<EduLearn.EnrollmentService.Consumers.EnrollmentCourseCreatedConsumer>();
    x.AddConsumer<EduLearn.EnrollmentService.Consumers.EnrollmentCourseDeletedConsumer>();
    x.AddConsumer<EduLearn.EnrollmentService.Consumers.PaymentCompletedConsumer>();
    x.AddConsumer<EduLearn.EnrollmentService.Consumers.ProgressUpdatedConsumer>();
});

// Database
builder.Services.AddDbContext<EnrollmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DI
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IEnrollmentService, EduLearn.EnrollmentService.Services.EnrollmentService>();
builder.Services.AddAutoMapper(cfg => {}, typeof(AutoMapperProfile));
builder.Services.AddAzureStorage();

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
        var db = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
        Console.WriteLine("[STARTUP] Applying Enrollment Database Migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("[STARTUP] EnrollmentService is ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] EnrollmentService Startup failed: {ex.Message}");
    }
}

app.Run();
