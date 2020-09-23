using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SafeParallelForEach.Tests
{
    public static class AsyncEnumerableProvider
    {
        public static async IAsyncEnumerable<int> Get(int number = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var i in Enumerable.Range(1, number))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                await Task.Delay(1, cancellationToken);
                yield return i; 
            }
        }
    }
}