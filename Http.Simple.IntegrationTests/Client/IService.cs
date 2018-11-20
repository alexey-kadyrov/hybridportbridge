using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;

namespace Http.Simple.IntegrationTests.Client
{
    internal interface IService
    {
        [Get("/api/echo/products/{id}")]
        Task<Product> GetProductAsync(int id);

        [Post("/api/echo/products")]
        Task<Product> PostProductAsync(Product product);

        [Post("/api/echo/large")]
        Task<HttpContent> PostLargeDataAsync(Stream data);
    }
}
