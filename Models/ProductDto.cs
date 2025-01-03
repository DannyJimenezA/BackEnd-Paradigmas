﻿namespace ProtectedApiProject.Models
{

    public class ProductDto
    {
        /*
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; } */
        public int ID { get; set; }
        public required string Name { get; set; }
        public double Price { get; set; }
        public int Count { get; set; }
        public string Category { get; set; }
        public int CommerceID { get; set; }
    }
}