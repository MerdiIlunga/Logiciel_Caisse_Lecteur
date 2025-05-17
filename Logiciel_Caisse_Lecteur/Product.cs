namespace PointOfSaleApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}