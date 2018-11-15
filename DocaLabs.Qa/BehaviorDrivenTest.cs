using System.Threading.Tasks;
using NUnit.Framework;

namespace DocaLabs.Qa
{
    [TestFixture]
    public abstract class BehaviorDrivenTest
    {
        [OneTimeSetUp]
        public async Task TryExecuteGivenAndWhen()
        {
            await Setup();
            await Given();
            await When();
        }

        [OneTimeTearDown]
        public Task TryCleanup()
        {
            try
            {
                return Cleanup();
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        protected virtual Task Setup()
        {
            return Task.CompletedTask;
        }

        protected abstract Task Given();
        protected abstract Task When();
        protected virtual Task Cleanup()
        {
            return Task.CompletedTask;
        }
    }
}
