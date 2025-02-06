namespace Mailo.Models
{
    public class Color
    {
        public int Id { get; set; }
        public string ColorName { get; set; }
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    }
}
