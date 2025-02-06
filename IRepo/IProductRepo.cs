using Mailo.Data.Enums;
using Mailo.Models;

namespace Mailo.IRepo
{
    public interface IProductRepo
    {
        Task<IEnumerable<Product>> GetAll();
        Task<Product> GetByID(int id, Sizes size);
    }
}
