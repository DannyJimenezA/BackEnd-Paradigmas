using EventStore.Client;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ProtectedApiProject.Hubs;
using System.Text;


const string connectionString = "esdb://admin:changeit@26.14.66.242:2113?tls=false&tlsVerifyCert=false";

var settings = EventStoreClientSettings.Create(connectionString);
settings.CreateHttpMessageHandler = () =>
    new SocketsHttpHandler
    {
        SslOptions =
        {
            RemoteCertificateValidationCallback = delegate { return true; }
        }
    };

var client = new EventStoreClient(settings);


var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// Configurar Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API protegida",
        Version = "v1"
    });

    // Configuración del esquema de seguridad JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese 'Bearer' [espacio] y luego el token en el cuadro de texto a continuación.\n\nEjemplo: \"Bearer abcdef12345\""
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configurar la autenticación con JWT Bearer
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://26.213.88.174:7113"; // URL de tu IdentityServer

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false
        };
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };


    });

builder.Services.AddHttpClient();

var app = builder.Build();

// Hub SignalR
var hubContext = app.Services.GetRequiredService<IHubContext<EventHub>>();
_ = ReadAllOldEventsAsync(client, hubContext);
_ = SubscribeToNewEventsAsync(client, hubContext);



// Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API protegida v1");
        c.DefaultModelsExpandDepth(-1); // Oculta el esquema de modelos en Swagger para mayor claridad
    });
}

app.UseHttpsRedirection();

// Asegurarse de usar el middleware de autenticación y autorización en el orden correcto
app.UseAuthentication(); // Habilita la autenticación
app.UseAuthorization(); // Habilita la autorización

app.MapControllers();
app.MapHub<EventHub>("/eventHub");

app.Run();


/*
async Task ReadAllOldEventsAsync(EventStoreClient client, IHubContext<EventHub> hubContext)
{
    Console.WriteLine("Cargando eventos antiguos:");
    var events = client.ReadStreamAsync(Direction.Forwards, "test", EventStore.Client.StreamPosition.Start);

    await foreach (var resolvedEvent in events)
    {
        var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
        Console.WriteLine($"Antiguo evento: {JsonConvert.SerializeObject(eventData)}");

        // Enviar evento a través de SignalR
        await hubContext.Clients.All.SendAsync("ReceiveEvent", eventData);
    }
}

async Task SubscribeToNewEventsAsync(EventStoreClient client, IHubContext<EventHub> hubContext)
{

    await client.SubscribeToStreamAsync("test", FromStream.End,
    async (subscription, resolvedEvent, cancellationToken) =>
    {
        var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
        Console.WriteLine($"Nuevo evento: {JsonConvert.SerializeObject(eventData)}");

        // Enviar nuevo evento a través de SignalR
        await hubContext.Clients.All.SendAsync("ReceiveEvent", eventData, cancellationToken);
    },
    subscriptionDropped: (subscription, reason, exception) =>
    {
        Console.WriteLine($"Suscripción cancelada. Razón: {reason}. Excepción: {exception?.Message}");
    });


} */



async Task ReadAllOldEventsAsync(EventStoreClient client, IHubContext<EventHub> hubContext)
{
    Console.WriteLine("Cargando eventos antiguos:");
    var events = client.ReadStreamAsync(Direction.Forwards, "test", EventStore.Client.StreamPosition.Start);
    var oldEvents = new List<string>(); // Store old events here

    await foreach (var resolvedEvent in events)
    {
        var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
        oldEvents.Add(eventData);
        Console.WriteLine($"Antiguo evento: {eventData}");
    }

    // Now send all old events at once (optional, can be sent in batches)
    await hubContext.Clients.All.SendAsync("ReceiveEvent", oldEvents);
}

async Task SubscribeToNewEventsAsync(EventStoreClient client, IHubContext<EventHub> hubContext)
{
    var threshold = DateTime.Now.AddDays(-7); // Events older than 7 days are considered "old"

    await client.SubscribeToStreamAsync("test", FromStream.End,
        async (subscription, resolvedEvent, cancellationToken) =>
        {
            var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
            var isNewEvent = resolvedEvent.Event.Created > threshold;

            Console.WriteLine($"{(isNewEvent ? "Nuevo" : "Antiguo")} evento: {eventData}");

            // Send event to frontend with "isNew" property
            await hubContext.Clients.All.SendAsync("ReceiveEvent", new { data = eventData, isNew = isNewEvent }, cancellationToken);
        },
        subscriptionDropped: (subscription, reason, exception) =>
        {
            Console.WriteLine($"Suscripción cancelada. Razón: {reason}. Excepción: {exception?.Message}");
        });
}
