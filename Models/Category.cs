using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Mailo.Models
{
    public class Category
    {
        public int Id { get; set; }
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]

        
        public string Name { get; set; }
        public ICollection<Product>? products { get; set; }
    }
}
