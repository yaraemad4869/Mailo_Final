namespace Mailo.Models
{
    public class CheckoutViewModel
	{
		public double TotalPrice { get; set; } 
		public string PromoCode { get; set; } 
		public double Discount { get; set; } 
		public double FinalPrice { get; set; }
	}
}
