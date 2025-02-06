using System.ComponentModel.DataAnnotations;

namespace Mailo.Models
{
    public class PromoCode
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } 

        [Required]
        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
        public double DiscountPercentage { get; set; } 

        public DateTime ExpiryDate { get; set; }

		public ICollection<Order>?  Orders { get; set; }
	}

}
