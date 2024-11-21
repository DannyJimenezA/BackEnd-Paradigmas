using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ProtectedApiProject.Hubs
{
    public class StatisticsHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Lógica cuando un cliente se conecta
            await base.OnConnectedAsync();
            Console.WriteLine($"Cliente conectado: {Context.ConnectionId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Lógica cuando un cliente se desconecta
            await base.OnDisconnectedAsync(exception); // Se pasa el parámetro exception
            Console.WriteLine($"Cliente desconectado: {Context.ConnectionId}, Error: {exception?.Message}");
        }
    }
}

