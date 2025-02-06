using Mailo.Data;
using Mailo.Models;
using Mailo.IRepo;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Repo
{
    public class UserInfoRepo : IUserInfoRepo
    {
        private readonly AppDbContext _db;
        public UserInfoRepo(AppDbContext db)
        {
            _db = db;
        }
        public async Task<User> GetUser(string? Email)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Email == Email);
        }
    }
}
