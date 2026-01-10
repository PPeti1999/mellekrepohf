using AnalyticsService.Consumers;
using MassTransit;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

// 1. MongoDB Konfiguráció
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://ticket-mongo:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDb = mongoClient.GetDatabase("TicketAnalyticsDb");

// Regisztráljuk az adatbázist Singletonként
builder.Services.AddSingleton(mongoDb);

// 2. MassTransit Konfiguráció
builder.Services.AddMassTransit(x =>
{
    // A TicketPurchasedConsumer-t regisztráljuk (amit a Consumers mappába teszünk)
    x.AddConsumer<TicketPurchasedConsumer>();

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

var host = builder.Build();
host.Run();