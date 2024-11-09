using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProtectedApiProject.Models;
using System.Net.Http;

namespace ProtectedApiProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        [Authorize(Roles = "Dashboard")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost("request-token")]
        public async Task<IActionResult> RequestToken([FromBody] TokenRequestModel model)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var client = new HttpClient(handler); // Usamos el cliente con validación flexible

            var tokenRequest = new PasswordTokenRequest
            {
                Address = "https://26.213.88.174:7113/connect/token", // Nueva URL de IdentityServer
                ClientId = "LogErrores",
                ClientSecret = "logerrores_secret",
                UserName = model.UserName,
                Password = model.Password,
                Scope = "logerrores_api role_access offline_access"
            };

            var tokenResponse = await client.RequestPasswordTokenAsync(tokenRequest);

            if (tokenResponse.IsError)
            {
                return BadRequest(tokenResponse.Error);
            }

            return Ok(tokenResponse.AccessToken);
        }

    }
}
