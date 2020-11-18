namespace SafeParallel.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class InputReturnTests
    {
        [Fact]
        public async Task EachItemIsProcessedOnce()
        {
            var inputValues = Enumerable.Range(1, 100);

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
            results.Select(r => r.Input).ShouldBe(inputValues, true);
            results.ShouldAllBe(r => r.Success);
        }

        [Fact]
        public async Task MaxParallelismDefaultIsRespected()
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
            await foreach (var result in inputValues.SafeParallelAsyncWithResult(action, parallelism))
            {
            }

            maxSeenParallelism.ShouldBe(parallelism);
        }

        [Fact]
        public async Task ReportsExceptions()
        {
            var inputValues = Enumerable.Range(1, 100);

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
            results.Select(r => r.Input).ShouldBe(inputValues, true);
        }

        [Fact]
        public async Task RespectsCancellationToken()
        {
            var inputValues = Enumerable.Range(1, 100);
            var cancellationTokenSource = new CancellationTokenSource();
            Func<int, Task> action = async (int i) =>
            {
                await Task.Delay(10);
            };

            ConcurrentBag<Result<int>> results = new ConcurrentBag<Result<int>>();

            cancellationTokenSource.CancelAfter(10);
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
