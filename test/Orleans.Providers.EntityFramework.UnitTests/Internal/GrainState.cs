using Orleans;

namespace Orleans.Providers.EntityFramework.UnitTests.Internal
{
    public class TestGrainState<T> : IGrainState<T>
        where T : class, new()
    {
        public T State { get; set; }

        public string ETag { get; set; }

        public bool RecordExists { get; set; }
    }
}