using System.Threading.Tasks;
using Refit;

namespace Resilience.IntegrationTests.Client
{
    internal interface IService
    {
        [Post("/api/echo/products")]
        Task<Product> PostProductAsync(Product product);
    }
}
