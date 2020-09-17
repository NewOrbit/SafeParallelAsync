namespace SafeParallelForEach.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class VoidReturnTests
    {
        [Fact]
        public void ConcurrentBagCanHaveSameValueMultipleTimes()
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            bag.Add(1);
            bag.Add(1);
            bag.Count().ShouldBe(2);
        }

        [Fact]
        public async Task EachItemIsProcessedOnce()
        {
            var inputValues = Enumerable.Range(1, 100);
            ConcurrentBag<int> usedValues = new ConcurrentBag<int>();
            Func<int, Task> action = async (int i) =>
            {
                await Task.Delay(10);
                usedValues.Add(i);
            };
            await inputValues.SafeParallel(action);
            usedValues.Count().ShouldBe(100);
            usedValues.ShouldBe(inputValues, true);
        }

        [Fact]
        public async Task MaxParallelismIsRespected()
        {
            var inputValues = Enumerable.Range(1, 100);
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

                await Task.Delay(10);
                parallelCounter.ShouldBeLessThanOrEqualTo(parallelism);
                Interlocked.Decrement(ref parallelCounter);
            };
            await inputValues.SafeParallel(action, parallelism);
            maxSeenParallelism.ShouldBe(parallelism);
        }

        [Fact]
        public async Task RespectCancellationToken()
        {
            var inputValues = Enumerable.Range(1, 100);
            var cancellationTokenSource = new CancellationTokenSource();
            ConcurrentBag<int> usedValues = new ConcurrentBag<int>();
            Func<int, Task> action = async (int i) =>
            {
                await Task.Delay(10);
                usedValues.Add(i);
            };
            var task = inputValues.SafeParallel(action, 10, cancellationTokenSource.Token);
            cancellationTokenSource.CancelAfter(1);
            await task;
            usedValues.Count().ShouldBeLessThan(100);
            usedValues.Count().ShouldBeGreaterThan(0);
        }

        //// TODO: Test cancellation token!
    }
}
