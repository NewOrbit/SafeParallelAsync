namespace SafeParallelForEach.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class AsyncOutputReturnTests
    {
        [Fact]
        public async Task EachItemIsProcessedOnce()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);

            Func<int, Task<int>> action = async (int i) =>
            {
                await Task.Delay(10);
                return i * 2;
            };

            ConcurrentBag<Result<int, int>> results = new ConcurrentBag<Result<int, int>>();

            await foreach (var result in inputValues.SafeParrallelWithResult(action))
            {
                results.Add(result);
            }

            results.Count().ShouldBe(100);
            results.Select(r => r.Input).ShouldBe(Enumerable.Range(1, 100), true);
            results.ShouldAllBe(r => r.Success);
        }

        [Fact]
        public async Task HandlesValueTypes()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);
            var expected = Enumerable.Range(1, 100).Sum(i => i) * 2;
            int actual = 0;

            Func<int, Task<int>> action = async (int i) =>
            {
                await Task.Delay(10);
                return i * 2;
            };

            await foreach (var result in inputValues.SafeParrallelWithResult(action))
            {
                actual += result.Output;
            }

            actual.ShouldBe(expected);
        }

        [Fact]
        public async Task HandlesReferenceTypes()
        {
            var inputValues = AsyncEnumerableProvider.GetObjects(100);
            var expected = Enumerable.Range(1, 100).Sum(i => i * 2);
            int actual = 0;

            Func<DummyClass, Task<DummyClass>> action = async (DummyClass input) =>
            {
                await Task.Delay(10);
                return new DummyClass(input.MyInt * 2);
            };

            await foreach (var result in inputValues.SafeParrallelWithResult(action))
            {
                actual += result.Output.MyInt;
            }

            actual.ShouldBe(expected);
        }

        [Fact]
        public async Task MaxParallelismIsRespected()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);
            int parallelism = 10;
            int maxSeenParallelism = 0;
            int parallelCounter = 0;
            Func<int, Task<int>> action = async (int i) =>
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
                return i * 2;
            };
            await foreach (var result in inputValues.SafeParrallelWithResult(action, parallelism))
            {
            }

            maxSeenParallelism.ShouldBe(parallelism);
        }

        [Fact]
        public async Task ReportsExceptionsWithValueTypes()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);

            Func<int, Task<int>> action = async (int i) =>
            {
                await Task.Delay(10);
                if (i % 10 == 0)
                {
                    throw new Exception("We don't like 10s");
                }

                return i * 2;
            };

            ConcurrentBag<Result<int, int>> results = new ConcurrentBag<Result<int, int>>();

            await foreach (var result in inputValues.SafeParrallelWithResult(action))
            {
                results.Add(result);
            }

            results.Count().ShouldBe(100);
            results.Count(r => r.Success).ShouldBe(90);
            results.Count(r => !r.Success).ShouldBe(10);
            results.First(r => !r.Success).Exception.ShouldNotBeNull();
            results.First(r => !r.Success).Exception.Message.ShouldBe("We don't like 10s");
            results.Select(r => r.Input).ShouldBe(Enumerable.Range(1, 100), true);
            results.First(r => !r.Success).Output.ShouldBe(default(int));
        }

        [Fact]
        public async Task ReportsExceptionsWithReferenceTypes()
        {
            var inputValues = AsyncEnumerableProvider.GetObjects(100);

            Func<DummyClass, Task<DummyClass>> action = async (DummyClass input) =>
            {
                await Task.Delay(10);
                if (input.MyInt % 10 == 0)
                {
                    throw new Exception("We don't like 10s");
                }

                return new DummyClass(input.MyInt * 2);
            };

            var results = new ConcurrentBag<Result<DummyClass, DummyClass>>();

            await foreach (var result in inputValues.SafeParrallelWithResult(action))
            {
                results.Add(result);
            }

            results.Count().ShouldBe(100);
            results.Count(r => r.Success).ShouldBe(90);
            results.Count(r => !r.Success).ShouldBe(10);
            results.First(r => !r.Success).Exception.ShouldNotBeNull();
            results.First(r => !r.Success).Exception.Message.ShouldBe("We don't like 10s");
            results.Select(r => r.Input.MyInt).ShouldBe(Enumerable.Range(1, 100), true);
            results.First(r => !r.Success).Output.ShouldBe(null);
        }

        [Fact]
        public async Task RespectsCancellationToken()
        {
            var inputValues = AsyncEnumerableProvider.GetInts(100);
            var cancellationTokenSource = new CancellationTokenSource();

            Func<int, Task<int>> action = async (int i) =>
            {
                await Task.Delay(50);
                return i * 2;
            };

            ConcurrentBag<Result<int, int>> results = new ConcurrentBag<Result<int, int>>();
            cancellationTokenSource.CancelAfter(500);
            await foreach (var result in inputValues.SafeParrallelWithResult(action, 10, cancellationTokenSource.Token))
            {
                results.Add(result);
            }

            results.Count().ShouldBeLessThan(100);
            results.Count().ShouldBeGreaterThan(0);
        }
    }
}
