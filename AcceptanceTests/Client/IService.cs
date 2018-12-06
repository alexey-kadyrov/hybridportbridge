using System.Threading.Tasks;
using Refit;

namespace AcceptanceTests.Client
{
    internal interface IService
    {
        [Get("/api/echo/products/{id}")]
        Task<Product> GetProductAsync(int id);

        [Post("/api/echo/products")]
        Task<Product> PostProductAsync(Product product);
    }
}
