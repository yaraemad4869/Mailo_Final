using Mailo.Models;

namespace Mailo.IRepo
{
    public interface IUserInfoRepo
    {
        Task<User> GetUser(string? Email);
    }
}
