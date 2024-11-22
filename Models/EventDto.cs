namespace ProtectedApiProject.Models
{
    public class EventDto
    {
       /* public string Message { get; set; }
        public int PurchaseId { get; set; }
        public int CustomerId { get; set; }
        public List<ProductDto> Products { get; set; }
        public DateTime Date { get; set; }
        public bool IsNew { get; set; } */


        public int ID { get; set; }
        public required List<ProductDto> Details { get; set; }
        public double Total { get; set; }
        public required User User { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime Time { get; set; }
    }
}
