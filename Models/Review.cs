
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Mailo.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public string? Content { get; set; }


        [Range(1, 5, ErrorMessage = "Review must be between 1, 5")]
        public int? Rating { get; set; }

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

        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [ForeignKey("User")]

        public int UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }
        public Product Product { get; set; }

    }
}
