using Mailo.Data;
using Mailo.Data.Enums;
using Mailo.Models;
using Mailo.IRepo;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Repo
{
    public class ProductRepo : IProductRepo
    {
        private readonly AppDbContext _db;
        public ProductRepo(AppDbContext db)
        {
            _db = db;
        }
        public async Task<IEnumerable<Product>> GetAll()
        {
            return await _db.Products.ToListAsync();
        }
        public async Task<Product> GetByID(int id,Sizes size)
        {
            return await _db.Products.FindAsync(id);
        }
    }
}
