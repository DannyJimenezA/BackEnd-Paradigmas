using EventStore.Client;
using Newtonsoft.Json;
using ProtectedApiProject.Models;
using System.Text;

namespace ProtectedApiProject.Services
{
    public class EventService : IEventService
    {
        private readonly EventStoreClient _eventStoreClient;

        public EventService(EventStoreClient eventStoreClient)
        {
            _eventStoreClient = eventStoreClient;
        }

        public async Task<List<EventDto>> GetOldEventsAsync()
        {
            var events = new List<EventDto>();

            // Leer eventos antiguos desde EventStoreDB
            var result = _eventStoreClient.ReadStreamAsync(
                Direction.Forwards,
                "test", // Reemplaza con el nombre del stream donde están tus eventos
                StreamPosition.Start
            );

            await foreach (var resolvedEvent in result)
            {
                var eventData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);

                // Deserializar los datos del evento
                var deserializedEvent = JsonConvert.DeserializeObject<EventDto>(eventData);
                if (deserializedEvent != null)
                {
                    events.Add(deserializedEvent);
                }
            }

            return events;
        }
    }
}
