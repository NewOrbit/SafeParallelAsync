namespace SafeParallel.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class AsyncInputReturnTests
    {
        [Fact]
        public async Task EachItemIsProcessedOnce()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);

            Func<int, Task> action = async (int i) =>
            {
                await Task.Delay(10);
            };

            ConcurrentBag<Result<int>> results = new ConcurrentBag<Result<int>>();

            await foreach (var result in inputValues.SafeParallelAsyncWithResult(action))
            {
                results.Add(result);
            }

            results.Count().ShouldBe(100);
            results.Select(r => r.Input).ShouldBe(Enumerable.Range(1, 100), true);
            results.ShouldAllBe(r => r.Success);
        }

        [Fact]
        public async Task MaxParallelismDefaultIsRespectedWhenPassedForSingleUse()
        {
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

                await Task.Delay(200);
                parallelCounter.ShouldBeLessThanOrEqualTo(parallelism);
                Interlocked.Decrement(ref parallelCounter);
            };
            await foreach (var result in inputValues.SafeParallelAsyncWithResult(action, parallelism))
            {
            }

            maxSeenParallelism.ShouldBe(parallelism);
        }

        [Fact]
        public async Task MaxParallelismDefaultIsRespectedWhenChangedGlobally()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);
            Parallelizer.MaxParallelismDefault = 10;
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

                await Task.Delay(200);
                parallelCounter.ShouldBeLessThanOrEqualTo(Parallelizer.MaxParallelismDefault);
                Interlocked.Decrement(ref parallelCounter);
            };
            await foreach (var result in inputValues.SafeParallelAsyncWithResult(action))
            {
            }

            maxSeenParallelism.ShouldBe(Parallelizer.MaxParallelismDefault);

            // Restore it's value to default
            Parallelizer.MaxParallelismDefault = 100;
        }

        [Fact]
        public async Task ReportsExceptions()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);

            Func<int, Task> action = async (int i) =>
            {
                await Task.Delay(10);
                if (i % 10 == 0)
                {
                    throw new Exception("We don't like 10s");
                }
            };

            ConcurrentBag<Result<int>> results = new ConcurrentBag<Result<int>>();

            await foreach (var result in inputValues.SafeParallelAsyncWithResult(action))
            {
                results.Add(result);
            }

            results.Count().ShouldBe(100);
            results.Count(r => r.Success).ShouldBe(90);
            results.Count(r => !r.Success).ShouldBe(10);
            results.First(r => !r.Success).Exception.ShouldNotBeNull();
            results.First(r => !r.Success).Exception.Message.ShouldBe("We don't like 10s");
            results.Select(r => r.Input).ShouldBe(Enumerable.Range(1, 100), true);
        }

        [Fact]
        public async Task RespectsCancellationToken()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);
            var cancellationTokenSource = new CancellationTokenSource();
            Func<int, Task> action = async (int i) =>
            {
                await Task.Delay(100);
            };

            ConcurrentBag<Result<int>> results = new ConcurrentBag<Result<int>>();

            cancellationTokenSource.CancelAfter(500);
            var asyncEnumerable = inputValues.SafeParallelAsyncWithResult(action, 10);
            await foreach (var result in asyncEnumerable.WithCancellation(cancellationTokenSource.Token))
            {
                results.Add(result);
            }

            results.Count().ShouldBeLessThan(100);
            results.Count().ShouldBeGreaterThan(0);
        }
    }
}
