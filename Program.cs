using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SatelliteTracker.Backend.Middleware;
using SatelliteTracker.Backend.Repositories;
using SatelliteTracker.Backend.Repositories.Interfaces;
using SatelliteTracker.Backend.Services;
using SatelliteTracker.Backend.Services.Interfaces;
using SatelliteTracker.Backend.Services.Gps;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SatelliteTracker API", Version = "v1" });
});

//for db
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация сервисов

//builder.Services.AddScoped<ISatelliteDataRepository, SatelliteDataRepository>();
builder.Services.Configure<GpsSettings>(builder.Configuration.GetSection("GpsSettings"));
builder.Services.AddSingleton<ISatelliteDataRepository, MockSatelliteDataRepository>();
builder.Services.AddScoped<INmeaParserService, NmeaParserService>();
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddHostedService<GpsDataBackgroundService>();
builder.Services.AddSingleton<IGpsReader, SimulatedGpsReader>();




// Health Checks
builder.Services.AddHealthChecks();
    //.AddDbContextCheck<AppDbContext>();

var app = builder.Build();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets();
app.UseMiddleware<WebSocketMiddleware>();

Console.WriteLine($"ENVIRONMENT: {builder.Environment.EnvironmentName}");

app.MapFallbackToFile("index.html");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
            }));
    }
});

app.Run();