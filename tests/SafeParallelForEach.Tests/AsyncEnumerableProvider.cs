namespace SafeParallelForEach.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public static class AsyncEnumerableProvider
    {
        internal static async IAsyncEnumerable<int> GetInts(int number = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var i in Enumerable.Range(1, number))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(1); // In real code this should pass in the cancellation token, but that will throw a TaskCancelledException which is undesirable in test code
                yield return i;
            }
        }

        internal static async IAsyncEnumerable<DummyClass> GetObjects(int number = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var i in Enumerable.Range(1, number))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(1);
                yield return new DummyClass(i); // In real code this should pass in the cancellation token, but that will throw a TaskCancelledException which is undesirable in test code
            }
        }
    }
}