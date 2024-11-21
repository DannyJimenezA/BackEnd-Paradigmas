using EventStore.Client;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ProtectedApiProject.Services;
using System.Text;
using System.Threading.Tasks;
namespace ProtectedApiProject.Hubs
{
    public class EventHub : Hub
    {
        /*private readonly IEventService _eventService; // Servicio para obtener los eventos

        // Inyecta el servicio que contiene la lógica para obtener los eventos antiguos
        public EventHub(IEventService eventService)
        {
            _eventService = eventService;
        }

        // Método existente para enviar mensajes
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);

        }

        // Nuevo método para enviar eventos antiguos al cliente que lo solicita
        public async Task RequestOldEvents()
        {
            // Llama al servicio para obtener los eventos antiguos
            var oldEvents = await _eventService.GetOldEventsAsync();
            // Envía los eventos antiguos solo al cliente que los solicitó
            await Clients.Caller.SendAsync("ReceiveEvent", oldEvents);
        } */

        private readonly EventStoreClient _eventStoreClient;

        public EventHub(EventStoreClient eventStoreClient)
        {
            _eventStoreClient = eventStoreClient;
        }

        public async Task RequestOldEvents()
        {
            // Leer eventos antiguos desde EventStore
            var events = new List<string>(); // Usaremos strings para enviar los eventos directamente

            var result = _eventStoreClient.ReadStreamAsync(
                Direction.Forwards,
                "test", // El nombre de tu stream en EventStore
                StreamPosition.Start
            );

            await foreach (var resolvedEvent in result)
            {
                // Decodificar los datos del evento como JSON (asumiendo que tus eventos están en JSON)
                var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
                events.Add(eventData); // Agregar el evento como string
            }

            // Enviar los eventos antiguos al cliente
            await Clients.Caller.SendAsync("ReceiveEvent", events);
        }
    }
}




