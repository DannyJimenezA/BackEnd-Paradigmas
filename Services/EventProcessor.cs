using EventStore.Client;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ProtectedApiProject.Hubs;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProtectedApiProject.Services
{
    public class EventProcessor
    {
        private readonly IHubContext<EventHub> _hubContext;
        private readonly HttpClient _httpClient;

        public EventProcessor(IHubContext<EventHub> hubContext, HttpClient httpClient)
        {
            _hubContext = hubContext;
            _httpClient = httpClient;
        }

        public async Task StartEventPollingAsync()
        {
            while (true)
            {
                // Llama al endpoint remoto de EventStoreConf para obtener el evento más reciente
                var response = await _httpClient.GetAsync("api/EventStore/subscribe");

                if (response.IsSuccessStatusCode)
                {
                    var eventData = await response.Content.ReadAsStringAsync();

                    // Envía el evento al frontend en tiempo real a través de SignalR
                    await _hubContext.Clients.All.SendAsync("ReceiveEvent", eventData);
                }

                // Espera unos segundos antes de volver a consultar
                await Task.Delay(5000);
            }
        }

    }
}
