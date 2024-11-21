//using EventStore.Client;
//using Microsoft.AspNetCore.SignalR;
//using Newtonsoft.Json;
//using ProtectedApiProject.Hubs;
//using ProtectedApiProject.Services;
//using System.Text;

//var builder = WebApplication.CreateBuilder(args);

//// Configurar EventStoreClient
//builder.Services.AddSingleton(sp =>
//{
//    var settings = EventStoreClientSettings.Create("esdb://admin:changeit@26.95.4.225:2113?tls=false");
//    settings.CreateHttpMessageHandler = () =>
//        new SocketsHttpHandler
//        {
//            SslOptions = { RemoteCertificateValidationCallback = delegate { return true; } }
//        };
//    return new EventStoreClient(settings);
//});

//// Agregar servicios al contenedor
//builder.Services.AddControllers();
//builder.Services.AddSignalR();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSingleton<IEventService, EventService>();
//builder.Services.AddSingleton<IEventDataService, EventDataService>(); // Servicio para almacenar eventos
//builder.Services.AddHttpClient();

//// Configurar CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowLocalhost", builder =>
//    {
//        builder.WithOrigins("http://localhost:5173") // URL del frontend local
//               .AllowAnyHeader()
//               .AllowAnyMethod()
//               .AllowCredentials(); // Necesario para SignalR
//    });
//});

//// Configurar Swagger
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
//    {
//        Title = "API protegida",
//        Version = "v1"
//    });

//    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
//        Scheme = "Bearer",
//        BearerFormat = "JWT",
//        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//        Description = "Ingrese 'Bearer' [espacio] y luego el token en el cuadro de texto a continuación.\n\nEjemplo: \"Bearer abcdef12345\""
//    });

//    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
//    {
//        {
//            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//            {
//                Reference = new Microsoft.OpenApi.Models.OpenApiReference
//                {
//                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            new string[] {}
//        }
//    });
//});

//// Configurar la autenticación con JWT Bearer
//builder.Services.AddAuthentication("Bearer")
//    .AddJwtBearer("Bearer", options =>
//    {
//        options.Authority = "https://26.213.88.174:7113"; // URL de tu IdentityServer
//        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
//        {
//            ValidateAudience = false
//        };
//        options.BackchannelHttpHandler = new HttpClientHandler
//        {
//            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
//        };
//    });

//// Configurar Kestrel
//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
//    {
//        httpsOptions.AllowAnyClientCertificate();
//    });
//});

//var app = builder.Build();

//// Hub SignalR
//var eventStoreClient = app.Services.GetRequiredService<EventStoreClient>();
//var hubContext = app.Services.GetRequiredService<IHubContext<EventHub>>();
//var eventDataService = app.Services.GetRequiredService<IEventDataService>();

//// Leer eventos antiguos y suscribirse a nuevos eventos
//_ = Task.Run(() => ReadAllOldEventsAsync(eventStoreClient, hubContext, eventDataService));
//_ = Task.Run(() => SubscribeToNewEventsAsync(eventStoreClient, hubContext, eventDataService));

//// Configurar el pipeline HTTP
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c =>
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API protegida v1");
//        c.DefaultModelsExpandDepth(-1); // Oculta el esquema de modelos en Swagger para mayor claridad
//    });
//}

//app.UseCors("AllowLocalhost");
//app.UseHttpsRedirection();
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();
//app.MapHub<EventHub>("/eventHub");

//app.Run();

//// Métodos Asíncronos para Manejo de Eventos
//async Task ReadAllOldEventsAsync(EventStoreClient client, IHubContext<EventHub> hubContext, IEventDataService eventDataService)
//{
//    Console.WriteLine("Cargando eventos antiguos...");
//    var events = client.ReadStreamAsync(Direction.Forwards, "test", StreamPosition.Start);

//    await foreach (var resolvedEvent in events)
//    {
//        var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
//        eventDataService.AddEvent(eventData); // Guardar evento
//        Console.WriteLine($"Evento antiguo: {eventData}");
//    }
//}

//async Task SubscribeToNewEventsAsync(EventStoreClient client, IHubContext<EventHub> hubContext, IEventDataService eventDataService)
//{
//    Console.WriteLine("Suscribiéndose a eventos nuevos...");
//    await client.SubscribeToStreamAsync("test", FromStream.End,
//        async (subscription, resolvedEvent, cancellationToken) =>
//        {
//            var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
//            eventDataService.AddEvent(eventData); // Guardar evento
//            Console.WriteLine($"Nuevo evento: {eventData}");
//            await hubContext.Clients.All.SendAsync("ReceiveEvent", new[] { eventData });
//        },
//        subscriptionDropped: (subscription, reason, exception) =>
//        {
//            Console.WriteLine($"Suscripción cancelada. Razón: {reason}. Excepción: {exception?.Message}");
//        });
//}

using EventStore.Client;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ProtectedApiProject.Hubs;
using ProtectedApiProject.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurar EventStoreClient
builder.Services.AddSingleton(sp =>
{
    var settings = EventStoreClientSettings.Create("esdb://admin:changeit@26.95.4.225:2113?tls=false");
    settings.CreateHttpMessageHandler = () =>
        new SocketsHttpHandler
        {
            SslOptions = { RemoteCertificateValidationCallback = delegate { return true; } }
        };
    return new EventStoreClient(settings);
});

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IEventDataService, EventDataService>(); // Servicio para almacenar eventos
builder.Services.AddHttpClient();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", builder =>
    {
        builder.WithOrigins("http://localhost:5173") // URL del frontend local
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials(); // Necesario para SignalR
    });
});

// Configurar Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API protegida",
        Version = "v1"
    });

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
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });

// Configurar Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.AllowAnyClientCertificate();
    });
});

var app = builder.Build();

// Hub SignalR
var eventStoreClient = app.Services.GetRequiredService<EventStoreClient>();
var hubContext = app.Services.GetRequiredService<IHubContext<StatisticsHub>>();
var eventDataService = app.Services.GetRequiredService<IEventDataService>();

// Leer eventos antiguos y suscribirse a nuevos eventos
_ = Task.Run(() => ReadAllOldEventsAsync(eventStoreClient, eventDataService));
_ = Task.Run(() => SubscribeToNewEventsAsync(eventStoreClient, eventDataService));

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

app.UseCors("AllowLocalhost");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<StatisticsHub>("/statisticsHub"); // Cambiado para usar el hub correcto

app.Run();

// Métodos Asíncronos para Manejo de Eventos
async Task ReadAllOldEventsAsync(EventStoreClient client, IEventDataService eventDataService)
{
    Console.WriteLine("Cargando eventos antiguos...");
    var events = client.ReadStreamAsync(Direction.Forwards, "test", StreamPosition.Start);

    await foreach (var resolvedEvent in events)
    {
        var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
        eventDataService.AddEvent(eventData); // Guardar evento y transmitir estadísticas
        Console.WriteLine($"Evento antiguo: {eventData}");
    }
}

async Task SubscribeToNewEventsAsync(EventStoreClient client, IEventDataService eventDataService)
{
    Console.WriteLine("Suscribiéndose a eventos nuevos...");
    await client.SubscribeToStreamAsync("test", FromStream.End,
        async (subscription, resolvedEvent, cancellationToken) =>
        {
            var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
            eventDataService.AddEvent(eventData); // Guardar evento y transmitir estadísticas
            Console.WriteLine($"Nuevo evento: {eventData}");
        },
        subscriptionDropped: (subscription, reason, exception) =>
        {
            Console.WriteLine($"Suscripción cancelada. Razón: {reason}. Excepción: {exception?.Message}");
        });
}
