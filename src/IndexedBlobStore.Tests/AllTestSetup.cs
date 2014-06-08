using Machine.Specifications;

namespace IndexedBlobStore.Tests
{
    public class AllTestSetup : IAssemblyContext
    {
        public void OnAssemblyStart()
        {
            TestContext.Setup();
        }

        public void OnAssemblyComplete()
        {
            TestContext.Current.Client.Delete();
        }
    }
}