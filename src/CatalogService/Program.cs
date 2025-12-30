using CatalogService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Szolgáltatások regisztrálása
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adatbázis kontextus hozzáadása (PostgreSQL)
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// 2. Automatikus adatbázis migráció és tábla létrehozás induláskor
// (Ez kényelmes fejlesztéshez, nem kell kézzel parancssorozni)
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

// 3. HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();