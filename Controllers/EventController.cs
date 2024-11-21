using Microsoft.AspNetCore.Mvc;
using ProtectedApiProject.Services;

namespace ProtectedApiProject.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventController : ControllerBase
    {
        private readonly IEventDataService _eventDataService;

        public EventController(IEventDataService eventDataService)
        {
            _eventDataService = eventDataService;
        }

        [HttpGet]
        public IActionResult GetEvents()
        {
            var statistics = _eventDataService.GetPurchaseStatistics();
            return Ok(statistics);
        }
    }
}

