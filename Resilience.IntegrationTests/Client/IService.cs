using System.Threading.Tasks;
using Refit;

namespace Resilience.IntegrationTests.Client
{
    public interface IService
    {
        [Post("/api/echo/products")]
        Task<Product> PostProductAsync(Product product);
    }
}
