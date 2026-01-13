using BookingService.Data;
using BookingService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BookingService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// auth konf
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
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

//  DB  Redis
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "ticket-cache:6379";
});

// HTTP Client catalogra
builder.Services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri("http://catalog-service:8080/"); 
})
.AddStandardResilienceHandler(); 

builder.Services.AddScoped<ICatalogClient, CatalogClient>();

//  MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<BookingDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });
    //consumer
    x.AddConsumer<EventUpdatedConsumer>(); 
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

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BookingDbContext>(); // BookingService esetén: BookingDbContext
        
        // Egyszerű Retry logika: 5 próbálkozás, 2 mp várakozással
        int retries = 5;
        while (retries > 0)
        {
            try
            {
                Console.WriteLine("Migráció indítása...");
                context.Database.Migrate();
                Console.WriteLine("Migráció sikeres!");
                break; 
            }
            catch (Exception ex)
            {
                retries--;
                Console.WriteLine($"Hiba a migrációnál (Még {retries} próba): {ex.Message}");
                if (retries == 0) throw; 
                System.Threading.Thread.Sleep(2000); // 2 mp
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Kritikus hiba: Nem sikerült az adatbázis kapcsolat: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();