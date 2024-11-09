using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
namespace ProtectedApiProject.Hubs
{
    public class EventHub : Hub
    {
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}

