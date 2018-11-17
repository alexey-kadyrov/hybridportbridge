using System.IO;
using System.Threading.Tasks;
using Refit;

namespace DocaLabs.HybridPortBridge.IntegrationTests.Service
{
    internal interface IService
    {
        [Get("/api/echo/products/{id}")]
        Task<Product> GetProductAsync(int id);

        [Post("/api/echo/products")]
        Task<Product> PostProductAsync(Product product);

        [Post("/api/echo/large")]
        Task<Stream> PostLargeDataAsync(Stream data);
    }
}
