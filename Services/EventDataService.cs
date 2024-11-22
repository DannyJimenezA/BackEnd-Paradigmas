using System.Collections.Concurrent;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;
using ProtectedApiProject.Hubs;
using ProtectedApiProject.Models;

namespace ProtectedApiProject.Services
{
   
    public interface IEventDataService
    {
        void AddEvent(EventDto eventData);
        IEnumerable<EventDto> GetAllEvents();
        object GetPurchaseStatistics(); // Estadísticas iniciales
        Task BroadcastStatistics(); // Método para enviar estadísticas en tiempo real
    }

    public class EventDataService : IEventDataService
    {
        private readonly ConcurrentBag<EventDto> _events = new();
        private readonly IHubContext<StatisticsHub> _hubContext;

        public EventDataService(IHubContext<StatisticsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void AddEvent(EventDto eventData)
        {
            _events.Add(eventData);
            _ = BroadcastStatistics(); // Transmitir actualizaciones en tiempo real
        }

        public IEnumerable<EventDto> GetAllEvents()
        {
            return _events;
        }

        public object GetPurchaseStatistics()
        {
            // Parsear los eventos a objetos
            var purchases = _events.ToList();

            // Producto más caro (Global)
            var mostExpensiveProduct = purchases
                .SelectMany(p => p.Details)
                .OrderByDescending(prod => prod.Price)
                .FirstOrDefault();

            // Provincia con más compras (Global)
            var topProvince = purchases
                .GroupBy(p => p.User.Provincia) // Agrupar por provincia del usuario
                .Select(g => new
                {
                    Provincia = g.Key,
                    TotalPurchases = g.Count()
                })
                .OrderByDescending(g => g.TotalPurchases)
                .FirstOrDefault();

            // Calcular estadísticas agrupadas por mes
            var groupedByMonth = purchases
                .GroupBy(p => new { p.Time.Year, p.Time.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalAmount = g.Sum(p => p.Details.Sum(prod => prod.Price)), // Suma de precios de todos los productos
                    TotalPurchases = g.Count(), // Cantidad de compras en el mes
                    TotalProducts = g.Sum(p => p.Details.Count), // Conteo total de productos comprados
                    TotalCustomers = g.Select(p => p.User.ID).Distinct().Count(), // Clientes únicos por mes
                    Products = g.SelectMany(p => p.Details) // Lista completa de productos
                                .GroupBy(prod => new { prod.ID, prod.Name }) // Agrupar productos por ID y Nombre
                                .Select(prodGroup => new
                                {
                                    ProductId = prodGroup.Key.ID,
                                    Name = prodGroup.Key.Name,
                                    Price = prodGroup.Sum(prod => prod.Price) // Total de precios por producto
                                })
                                .ToList()
                })
                .ToList();

            // Calcular estadísticas agrupadas por año
            var groupedByYear = purchases
                .GroupBy(p => p.Time.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    MaxSalesMonth = g.GroupBy(p => p.Time.Month)
                                     .Select(monthGroup => new
                                     {
                                         Month = monthGroup.Key,
                                         TotalSales = monthGroup.Sum(p => p.Details.Sum(prod => prod.Price))
                                     })
                                     .OrderByDescending(m => m.TotalSales)
                                     .FirstOrDefault(), // Mes con más ventas
                    MinSalesMonth = g.GroupBy(p => p.Time.Month)
                                     .Select(monthGroup => new
                                     {
                                         Month = monthGroup.Key,
                                         TotalSales = monthGroup.Sum(p => p.Details.Sum(prod => prod.Price))
                                     })
                                     .OrderBy(m => m.TotalSales)
                                     .FirstOrDefault() // Mes con menos ventas
                })
                .Select(y => new
                {
                    Year = y.Year,
                    MaxSalesMonth = new
                    {
                        Month = $"{y.Year}-{y.MaxSalesMonth.Month:D2}",
                        TotalAmount = y.MaxSalesMonth.TotalSales
                    },
                    MinSalesMonth = new
                    {
                        Month = $"{y.Year}-{y.MinSalesMonth.Month:D2}",
                        TotalAmount = y.MinSalesMonth.TotalSales
                    }
                })
                .ToList();

            return new
            {
                PurchasesByMonth = groupedByMonth,
                SalesStatisticsByYear = groupedByYear,
                MostExpensiveProduct = mostExpensiveProduct != null
            ? new
            {
                ProductId = mostExpensiveProduct.ID,
                ProductName = mostExpensiveProduct.Name,
                ProductPrice = mostExpensiveProduct.Price,
                ProductCount = mostExpensiveProduct.Count, // Número de productos en la compra
                ProductCategory = mostExpensiveProduct.Category, // Categoría del producto
                CommerceId = mostExpensiveProduct.CommerceID
            }
            : null,
                TopProvince = topProvince != null
            ? new
            {
                Province = topProvince.Provincia,
                TotalPurchases = topProvince.TotalPurchases
            }
            : null

            };
        }

        public async Task BroadcastStatistics()
        {
            var statistics = GetPurchaseStatistics();
            await _hubContext.Clients.All.SendAsync("ReceiveStatistics", statistics);
        }
    }
}
