namespace ProtectedApiProject.Models
{
    public class EventDto
    {
        public string Message { get; set; }
        public int PurchaseId { get; set; }
        public int CustomerId { get; set; }
        public List<ProductDto> Products { get; set; }
        public DateTime Date { get; set; }
        public bool IsNew { get; set; }

    }
}
