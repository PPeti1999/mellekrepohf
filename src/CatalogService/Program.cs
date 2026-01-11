using CatalogService.Data;
using CatalogService.Consumers; // <--- FONTOS: Ha MassTransit is kell
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// 1. DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. MassTransit (RabbitMQ) - Ha van
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TicketPurchasedEventConsumer>(); // Ha van consumered
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 3. JWT KONFIGURÁCIÓ (EZ HIÁNYZOTT!) ---
var jwtKey = builder.Configuration["JWT:Key"] ?? "EzEgyNagyonHosszuEsTitkosKulcsAmiLegalabb32Karakter2026";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,   // Egyszerűsítés miatt kikapcsolva
        ValidateAudience = false  // Egyszerűsítés miatt kikapcsolva
    };
});
// ---------------------------------------------

var app = builder.Build();

// --- 4. AUTOMATIKUS MIGRÁCIÓ (AZ 500-AS HIBA ELLEN) ---
// --- 4. AUTOMATIKUS MIGRÁCIÓ (JAVÍTOTT: RETRY LOGIKA) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CatalogDbContext>(); // BookingService esetén: BookingDbContext
        
        // Egyszerű Retry logika: 5 próbálkozás, 2 mp várakozással
        int retries = 5;
        while (retries > 0)
        {
            try
            {
                Console.WriteLine("Migráció indítása...");
                context.Database.Migrate();
                Console.WriteLine("Migráció sikeres!");
                break; // Ha sikerült, kilépünk a ciklusból
            }
            catch (Exception ex)
            {
                retries--;
                Console.WriteLine($"Hiba a migrációnál (Még {retries} próba): {ex.Message}");
                if (retries == 0) throw; // Ha elfogyott a próba, eldobjuk a hibát
                System.Threading.Thread.Sleep(2000); // 2 másodperc várakozás
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Kritikus hiba: Nem sikerült az adatbázis kapcsolat: {ex.Message}");
    }
}
// ------------------------------------------------------
// ------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // <--- FONTOS: Ez is kell!
app.UseAuthorization();

app.MapControllers();

app.Run();