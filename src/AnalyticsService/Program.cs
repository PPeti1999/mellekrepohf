using Contracts; // Most már a közös névteret használjuk
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

// 1. MongoDB Kapcsolat
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://ticketmaster-mongo:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase("TicketMasterAnalytics");
// Regisztráljuk a collection-t
builder.Services.AddSingleton(mongoDatabase.GetCollection<BsonDocument>("TicketPurchases"));

// 2. MassTransit (RabbitMQ) Konfiguráció
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TicketPurchasedAnalyticsConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Környezeti változóból olvassuk a hostot (alapértelmezés: rabbitmq)
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("analytics-service-queue", e =>
        {
            e.ConfigureConsumer<TicketPurchasedAnalyticsConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();

// 3. A Consumer osztály (Ez végzi a mentést)
public class TicketPurchasedAnalyticsConsumer : IConsumer<TicketPurchasedEvent>
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<TicketPurchasedAnalyticsConsumer> _logger;

    public TicketPurchasedAnalyticsConsumer(IMongoCollection<BsonDocument> collection, ILogger<TicketPurchasedAnalyticsConsumer> logger)
    {
        _collection = collection;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketPurchasedEvent> context)
    {
        var message = context.Message;
        // JAVÍTVA: message.CustomerEmail a message.UserId helyett
        _logger.LogInformation("Analytics: Új vásárlás rögzítése MongoDB-be. EventId: {EventId}, Email: {Email}", message.EventId, message.CustomerEmail);

        var document = new BsonDocument
        {
            { "BookingId", message.BookingId.ToString() },
            { "EventId", message.EventId.ToString() },
            { "EventName", message.EventName },
            { "UserEmail", message.CustomerEmail }, // A CustomerEmailt mentjük le
            { "TicketCount", message.TicketCount },
            { "PurchaseDate", DateTime.UtcNow },
            { "Source", "RabbitMQ" }
        };

        await _collection.InsertOneAsync(document);
    }
}