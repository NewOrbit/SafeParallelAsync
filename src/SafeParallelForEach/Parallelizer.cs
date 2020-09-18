namespace SafeParallelForEach
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Parallelizer
    {
        /// <summary>
        /// Runs the action over all the input items.
        /// There is no error handling - if the action throws an exception, it will blow and stop processing eventually.
        /// If desired you can put error handling in the action.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with
        /// streaming IEnumerables that you are reading from an external source.
        /// </summary>
        public static async Task SafeParallel<TIn>(this IEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
        {
            if (inputValues is null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var taskQueue = new Queue<Task>();

            // var tl = new List<Task>();
            var sem = new SemaphoreSlim(maxParallelism);
            foreach (var input in inputValues)
            {
                try
                {
                    await sem.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var task = action(input);
                taskQueue.Enqueue(RunIt(task, sem));

                // something like while stack.peek.iscompleted yield return? No, that will pause it. But, I could at least await it... Though, if I do yield return but only when it's done..? I want a pipeline really 'cause this is all about buffering but with yield return it just comes down to how fast the consumer consumes it. So that's probably ok???
                while (taskQueue.Peek().IsCompleted)
                {
                    await taskQueue.Dequeue();
                }
            }

            await Task.WhenAll(taskQueue);
        }

        public static IAsyncEnumerable<Result<TIn>> SafeParrallelWithResult<TIn>(this IEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return SafeParrallelWithResult<TIn, Result<TIn>>(inputValues, (TIn input, SemaphoreSlim sem) => RunIt(input, action, sem), maxParallelism, cancellationToken);
        }

        public static IAsyncEnumerable<Result<TIn, TOut>> SafeParrallelWithResult<TIn, TOut>(this IEnumerable<TIn> inputValues, Func<TIn, Task<TOut>> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return SafeParrallelWithResult<TIn, Result<TIn, TOut>>(inputValues, (TIn input, SemaphoreSlim sem) => RunIt(input, action, sem), maxParallelism, cancellationToken);
        }

        private static async IAsyncEnumerable<TResult> SafeParrallelWithResult<TIn, TResult>(IEnumerable<TIn> inputValues, Func<TIn, SemaphoreSlim, Task<TResult>> runner, int maxParallelism = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (inputValues is null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            var taskQueue = new Queue<Task<TResult>>();

            var sem = new SemaphoreSlim(maxParallelism);
            foreach (var input in inputValues)
            {
                try
                {
                    await sem.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                taskQueue.Enqueue(runner(input, sem));

                // Return the tasks that have already compleed
                while (taskQueue.Peek().IsCompleted)
                {
                    // As far as I can fathom, there is no way this could throw an exception so not handling it
                    yield return await taskQueue.Dequeue();
                }
            }

            foreach (var task in taskQueue)
            {
                yield return await task;
            }
        }

        private static async Task<Result<TIn>> RunIt<TIn>(TIn input, Func<TIn, Task> action, SemaphoreSlim sem)
        {
            #pragma warning disable CA1031
            try
            {
                await action(input);
                return new Result<TIn>(input);
            }
            catch (Exception e)
            {
                return new Result<TIn>(input, e);
            }
            finally
            {
                sem.Release();
            }
            #pragma warning restore CA1031
        }

        private static async Task<Result<TIn, TOut>> RunIt<TIn, TOut>(TIn input, Func<TIn, Task<TOut>> action, SemaphoreSlim sem)
        {
            #pragma warning disable CA1031
            try
            {
                TOut output = await action(input);
                return new Result<TIn, TOut>(input, output);
            }
            catch (Exception e)
            {
                return new Result<TIn, TOut>(input, e);
            }
            finally
            {
                sem.Release();
            }
            #pragma warning restore CA1031
        }

        private static async Task RunIt(Task task, SemaphoreSlim sem)
        {
            await task;
            sem.Release();
        }
    }
}