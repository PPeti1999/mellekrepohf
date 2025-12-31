using BookingService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // <--- EZT ADD HOZZÁ BIZTONSÁG KEDVÉÉRT!

var builder = WebApplication.CreateBuilder(args);

// --- Szolgáltatások regisztrálása ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Adatbázis (Postgres)
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 3. MassTransit (RabbitMQ)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

// 4. HttpClient + Polly (Hibatűrés)
// JAVÍTOTT RÉSZ:
builder.Services.AddHttpClient("catalog") // Adunk neki egy nevet, bár nem kötelező, de segít
       .AddStandardResilienceHandler();   // Így már meg kell találnia a 8.10.0 verzióval

// --- Build ---
var app = builder.Build();

// --- Middleware és Migráció ---

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    try { db.Database.Migrate(); } catch { }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();