using EduLearn.ContentService.Data;
using EduLearn.ContentService.Repositories;
using EduLearn.ContentService.Services;
using EduLearn.SharedLib.Extensions;
using EduLearn.SharedLib.Middlewares;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// DB Context
builder.Services.AddDbContext<ContentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Shared Lib Extensions
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddSharedSwagger("EduLearn Content Service");
builder.Services.AddAzureStorage();
builder.Services.AddSharedMessaging(builder.Configuration, x => 
{
    x.AddConsumer<EduLearn.ContentService.Consumers.CourseDeletedConsumer>();
    x.AddConsumer<EduLearn.ContentService.Consumers.CourseCreatedConsumer>();
});
builder.Services.AddAutoMapper(cfg => {}, typeof(EduLearn.ContentService.Mappings.ContentProfile));

// Repositories & Services
builder.Services.AddScoped<IContentRepository, ContentRepository>();
builder.Services.AddScoped<IContentService, ContentService>();

// Error Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// CORS for local file serving
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("PreviewPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10; // Max 10 requests per minute
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStatusCodePages();
app.UseExceptionHandler();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Database Migration at Startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        Console.WriteLine("[STARTUP] Applying Content Database Migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("[STARTUP] ContentService is ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] ContentService Startup failed: {ex.Message}");
    }
}

app.Run();
