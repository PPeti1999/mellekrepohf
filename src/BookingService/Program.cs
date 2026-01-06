using BookingService.Data;
using MassTransit;
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
// -------------------------

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
    });
});
// 4. HttpClient + Polly (Hibatűrés)
// JAVÍTOTT RÉSZ:
builder.Services.AddHttpClient("catalog") // Adunk neki egy nevet, bár nem kötelező, de segít
       .AddStandardResilienceHandler();   // Így már meg kell találnia a 8.10.0 verzióval

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