using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DocaLabs.Qa
{
    public abstract class ServiceBehaviorDrivenTest<TService> : BehaviorDrivenTest
    {
        protected IServiceScope Scope { get; private set; }

        protected TService Service { get; private set; }

        protected override Task Setup()
        {
            Scope = ServiceLocator.GetScope();
            Service = GetComponent<TService>();
            return Task.CompletedTask;
        }

        protected override Task Cleanup()
        {
            Scope.Dispose();
            return Task.CompletedTask;
        }

        protected TComponent GetComponent<TComponent>()
        {
            return Scope.ServiceProvider.GetService<TComponent>();
        }
    }
}