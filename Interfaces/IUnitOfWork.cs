using HomevoAPI.Models;

namespace HomevoAPI.Interfaces
{
    public interface IUnitOfWork
    {
        IGenericRepository<Product> Products { get; }
        IGenericRepository<Category> Categories { get; }
        IGenericRepository<Order> Orders { get; }
        IGenericRepository<OrderItem> OrderItems { get; }
        IGenericRepository<Favorite> Favorites { get; }

        Task<int> SaveChangesAsync();
    }
}