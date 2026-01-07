using BookingService.Data;
using MassTransit;
using BookingService.Services; // <--- NE FELEJTSD EL
using Microsoft.EntityFrameworkCore;
// ÚJ: Auth importok
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- AUTH KONFIGURÁCIÓ ---
var jwtKey = builder.Configuration["JWT:Key"] ?? "EzEgyNagyonHosszuEsTitkosKulcsAmiLegalabb32Karakter2026";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TicketMasterAuth",
            ValidAudience = "TicketMasterServices",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
// 1. ADATBÁZIS (PostgreSQL)
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. REDIS (Cache)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 3. MASSTRANSIT (RabbitMQ + Outbox)
builder.Services.AddMassTransit(x =>
{
    // --- ÚJ RÉSZ: Transactional Outbox ---
    // Ez biztosítja, hogy az üzenetküldés és az DB mentés atomi legyen.
    x.AddEntityFrameworkOutbox<BookingDbContext>(o =>
    {
        // Mivel fent UseNpgsql-t használtál, itt is Postgres kell!
        // Ha SQL Servert használnál, akkor o.UseSqlServer();
        o.UsePostgres(); 
        
        o.UseBusOutbox(); // Az üzeneteket először a DB-be menti, onnan küldi ki
    });
    // --------------------------------------

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        
        cfg.ConfigureEndpoints(context); // Fontos a Consumer-eknek
    });
});
// 4. HTTP CLIENT + POLLY (JAVÍTVA!)
// A sima AddHttpClient("catalog") helyett Typed Client-et használunk,
// így a Controllerbe már az ICatalogClient kerül injektálásra.
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(client =>
{
    // Ez a Docker Service neve a compose-ban és a belső portja
    client.BaseAddress = new Uri("http://catalog-service:8080"); 
})
.AddStandardResilienceHandler(); // Automatikus Retry policy (Polly)
// -------------------------------------
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

// SORREND FONTOS!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();