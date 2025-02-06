using Mailo.Data.Enums;
using Mailo.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mailo.Models
{
    [NotMapped]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [MaxLength(30)]
		public string Name { get; set; }
        [MaxLength(100)]
        public string? Description { get; set; }
        
        
        [MaxLength(50)]
        public string AdditionDate { get; set; } = DateTime.Now.ToString();
        public double? Discount { get; set; } = 0;
        public double TotalPrice { get; set; }
        public double Price { get; set; }
        //public int SQuantity { get; set; } = 0;
        //public int MQuantity { get; set; } = 0;
        //public int LQuantity { get; set; } = 0;
        public string? ImageUrl { get; set; }
        [NotMapped]
        public IFormFile? clientFile { get; set; }
        public byte[]? dbImage { get; set; }
        [NotMapped]
        public string? imageSrc
        {
            get
            {
                if (dbImage != null)
                {
                    string base64String = Convert.ToBase64String(dbImage, 0, dbImage.Length);
                    return "data:image/jpg;base64," + base64String;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
            }
        }
        [ForeignKey("CategoryId")]
        public int CategoryId { get; set; }
        public Category? Categorys { get; set; }
        public ICollection<OrderProduct>? OrderProducts { get; set; }
        public ICollection<Wishlist>? wishlists { get; set; }
        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        public ICollection<Review>? Reviews { get; set; }
       
       

    }
}
