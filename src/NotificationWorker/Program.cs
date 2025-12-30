using MassTransit;
using NotificationWorker;

var builder = Host.CreateApplicationBuilder(args);

// MassTransit és RabbitMQ konfiguráció
builder.Services.AddMassTransit(x =>
{
    // Regisztráljuk a fogyasztót (Consumer), hogy tudja, hova kell kézbesíteni az üzenetet
    x.AddConsumer<NotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Kapcsolódási adatok. 
        // Development környezetben (lokálisan) localhost.
        // Dockerben majd környezeti változóból jön.
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        // Ez a parancs hozza létre automatikusan a Queue-t a RabbitMQ-ban
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();