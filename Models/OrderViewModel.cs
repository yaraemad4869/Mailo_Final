namespace Mailo.Models
{
    public class OrderViewModel
    {
        public string? PromoCode { get; set; }
        public double TotalPrice { get; set; }
        public double  Discount { get; set; }
        public double FinalPrice { get; set; }
    }
}
