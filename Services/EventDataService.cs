using System.Collections.Concurrent;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;
using ProtectedApiProject.Hubs;

namespace ProtectedApiProject.Services
{
    public class PurchaseEvent
    {
        public string CustomerId { get; set; } // ID del cliente
        public DateTime Date { get; set; } // Fecha de la compra
        public List<Product> Products { get; set; } // Lista de productos
    }

    public class Product
    {
        public string ProductId { get; set; } // ID del producto
        public string Name { get; set; } // Nombre del producto
        public decimal Price { get; set; } // Precio del producto
    }

    public interface IEventDataService
    {
        void AddEvent(string eventData);
        IEnumerable<string> GetAllEvents();
        object GetPurchaseStatistics(); // Estadísticas iniciales
        Task BroadcastStatistics(); // Método para enviar estadísticas en tiempo real
    }

    public class EventDataService : IEventDataService
    {
        private readonly ConcurrentBag<string> _events = new();
        private readonly IHubContext<StatisticsHub> _hubContext;

        public EventDataService(IHubContext<StatisticsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void AddEvent(string eventData)
        {
            _events.Add(eventData);
            _ = BroadcastStatistics(); // Transmitir actualizaciones en tiempo real
        }

        public IEnumerable<string> GetAllEvents()
        {
            return _events;
        }

        public object GetPurchaseStatistics()
        {
            // Parsear los eventos a objetos
            var purchases = _events
                .Select(e => JsonConvert.DeserializeObject<PurchaseEvent>(e))
                .Where(e => e != null)
                .ToList();

            // Calcular estadísticas agrupadas por mes
            var groupedByMonth = purchases
                .GroupBy(p => new { p.Date.Year, p.Date.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalAmount = g.Sum(p => p.Products.Sum(prod => prod.Price)), // Suma de precios de todos los productos
                    TotalPurchases = g.Count(), // Cantidad de compras en el mes
                    TotalProducts = g.Sum(p => p.Products.Count), // Conteo total de productos comprados
                    TotalCustomers = g.Select(p => p.CustomerId).Distinct().Count(), // Clientes únicos por mes
                    Products = g.SelectMany(p => p.Products) // Lista completa de productos
                                .GroupBy(prod => new { prod.ProductId, prod.Name }) // Agrupar productos por ID y Nombre
                                .Select(prodGroup => new
                                {
                                    ProductId = prodGroup.Key.ProductId,
                                    Name = prodGroup.Key.Name,
                                    Price = prodGroup.Sum(prod => prod.Price) // Total de precios por producto
                                })
                                .ToList()
                })
                .ToList();

            // Calcular estadísticas agrupadas por año
            var groupedByYear = purchases
                .GroupBy(p => p.Date.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    MaxSalesMonth = g.GroupBy(p => p.Date.Month)
                                     .Select(monthGroup => new
                                     {
                                         Month = monthGroup.Key,
                                         TotalSales = monthGroup.Sum(p => p.Products.Sum(prod => prod.Price))
                                     })
                                     .OrderByDescending(m => m.TotalSales)
                                     .FirstOrDefault(), // Mes con más ventas
                    MinSalesMonth = g.GroupBy(p => p.Date.Month)
                                     .Select(monthGroup => new
                                     {
                                         Month = monthGroup.Key,
                                         TotalSales = monthGroup.Sum(p => p.Products.Sum(prod => prod.Price))
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
                SalesStatisticsByYear = groupedByYear
            };
        }

        public async Task BroadcastStatistics()
        {
            var statistics = GetPurchaseStatistics();
            await _hubContext.Clients.All.SendAsync("ReceiveStatistics", statistics);
        }
    }
}
