namespace SafeParallel.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class AsyncVoidReturnTests
    {
        [Fact]
        public async Task EachItemIsProcessedOnce()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);
            ConcurrentBag<int> usedValues = new ConcurrentBag<int>();
            Func<int, Task> action = async (int i) =>
            {
                await Task.Delay(10);
                usedValues.Add(i);
            };
            await inputValues.SafeParallelAsync(action);
            usedValues.Count().ShouldBe(100);
            usedValues.ShouldBe(Enumerable.Range(1, 100), true);
        }

        [Fact]
        public async Task MaxParallelismIsRespected()
        {
            ThreadPool.SetMinThreads(100, 100);
            var inputValues = AsyncEnumerableProvider.GetInts(100);
            int parallelism = 10;
            int maxSeenParallelism = 0;
            int parallelCounter = 0;
            Func<int, Task> action = async (int i) =>
            {
                Interlocked.Increment(ref parallelCounter);
                if (parallelCounter > maxSeenParallelism)
                {
                    // This is not threadsafe but should be good enough for this
                    maxSeenParallelism = parallelCounter;
                }

                await Task.Delay(200);  // Need substantially slower work here to see the parallelism
                parallelCounter.ShouldBeLessThanOrEqualTo(parallelism);
                Interlocked.Decrement(ref parallelCounter);
            };
            await inputValues.SafeParallelAsync(action, parallelism);
            maxSeenParallelism.ShouldBe(parallelism);
        }

        [Fact]
        public async Task RespectCancellationToken()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);
            var cancellationTokenSource = new CancellationTokenSource();
            ConcurrentBag<int> usedValues = new ConcurrentBag<int>();
            Func<int, Task> action = async (int i) =>
            {
                await Task.Delay(200);
                usedValues.Add(i);
            };
            var task = inputValues.SafeParallelAsync(action, 10, cancellationTokenSource.Token);
            cancellationTokenSource.CancelAfter(500);
            await task;
            usedValues.Count().ShouldBeLessThan(100);
            usedValues.Count().ShouldBeGreaterThan(0);
        }
    }
}
