using ProtectedApiProject.Models;

namespace ProtectedApiProject.Services
{
    public interface IEventService
    {
        Task<List<EventDto>> GetOldEventsAsync();
    }
}
