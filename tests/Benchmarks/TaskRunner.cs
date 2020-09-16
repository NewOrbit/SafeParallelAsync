namespace Benchmarks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using SafeParallelForEach;

    [MemoryDiagnoser]
    public class TaskRunner
    {
        private static IList<int> inputdata = Enumerable.Range(1, 100000).ToList();

        [Benchmark]
        public async Task JustRunIt()
        {
            await inputdata.SafeParallel(async i => await Task.Delay(10), 1000);
        }
    }
}