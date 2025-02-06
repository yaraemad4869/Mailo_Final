using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mailo.Data.Enums;

namespace Mailo.Models
{
    public class Order
    {
        [Key]
        public int ID { get; set; }
        [DisplayName("Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;//
        [DisplayName("Order Price")]

        public double OrderPrice { get; set; }
        [DisplayName("Delivery Fee")]

        public double DeliveryFee { get; set; } = 100;
        public double Discount;
		[DisplayName("Total Price")]
   
        public double TotalPrice { get; set; }

		public string? PromoCodeUsed { get; set; }

		[MaxLength(100)]
        [DisplayName("Order Address")]

        public string OrderAddress { get; set; }
		public double FinalPrice { get; set; }
		[DisplayName("Order Status")]

        public OrderStatus OrderStatus { get; set; } = OrderStatus.New;
        public int UserID { get; set; }
        public User user { get; set; }
        public int? EmpID { get; set; }
        public Employee? employee { get; set; }
        [ForeignKey("PromoCode")]
        public int? PromoCodeId { get; set; }
        public PromoCode? PromoCode { get; set; }
        public ICollection<OrderProduct>? OrderProducts { get; set; }
        public Payment? Payment { get; set; }

    }
}
