using CatalogService.Consumers;
using CatalogService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Szolgáltatások regisztrálása (MINDIG A BUILD ELŐTT!) ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adatbázis
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MassTransit / RabbitMQ (EZT FELHOZTAM IDE)
builder.Services.AddMassTransit(x =>
{
    // Regisztráljuk a fogyasztót
    x.AddConsumer<TicketPurchasedEventConsumer>();

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

// --- 2. Alkalmazás felépítése (EZ UTÁN MÁR NEM LEHET AddService-t hívni) ---
var app = builder.Build();

// --- 3. Futásidejű logika (Middleware, Migráció) ---

// Automatikus migráció
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    try 
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"HIBA a migráció során: {ex.Message}");
    }
}

// HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();