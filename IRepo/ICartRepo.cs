using Mailo.Data.Enums;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mailo.IRepo
{
    public interface ICartRepo
    {

        Task<List<Product>> GetProducts(Order order);
        Task<OrderProduct> IsMatched(Order order, int id, ProductVariant varient);
        Task<Order> GetOrder(User user);
        Task<OrderProduct> ExistingCartItem(int productId, ProductVariant varient, User user);

        void InsertToCart(int OrderId, int ProductId, ProductVariant varient);
        void DeleteFromCart(int OrderId, int ProductId, ProductVariant varient);
        Task<Order> GetOrCreateCart(User user);
        Task<List<Order>> GetOrders(User user);


    }
}