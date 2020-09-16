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
        /// <param name="inputValues">The IEnumerable you want actions carried out on.</param>
        /// <param name="action">The async action you want carried out on each item in inputValues.</param>
        /// <param name="maxParallelism">How many tasks should be allowed to run concurrently.</param>
        /// <param name="cancellationToken">An optional cancellation token you can use to stop the processing. This will stop more tasks being enqued, but will allow tasks already started to complete.</param>
        /// <typeparam name="TIn">The type of input values you will supply.</typeparam>
        /// <returns>A <see cref="Task"/> representing the parallel operation.</returns>
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

            var sem = new SemaphoreSlim(maxParallelism);
            foreach (var input in inputValues)
            {
                await sem.WaitAsync();
                var task = action(input);
                taskQueue.Enqueue(RunIt(task, sem));

                while (taskQueue.Peek().IsCompleted)
                {
                    await taskQueue.Dequeue();
                }
            }

            await Task.WhenAll(taskQueue);
        }

        /// <summary>
        /// Runs the action over all the input items.
        /// Errors will be caught and returned in the IAsyncEnumerable.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with
        /// streaming IEnumerables that you are reading from an external source.
        /// However, this will allocate a Result object for each Input so will inherently do more allocations than <see cref="SafeParallel{TIn}(IEnumerable{TIn}, Func{TIn, Task}, int, CancellationToken)"/>.
        /// Ensure you iterate through the results as they are being processed! If you don't consume the results, the processing will stop.
        /// </summary>
        /// <param name="inputValues">The IEnumerable you want actions carried out on.</param>
        /// <param name="action">The async action you want carried out on each item in inputValues.</param>
        /// <param name="maxParallelism">How many tasks should be allowed to run concurrently.</param>
        /// <param name="cancellationToken">An optional cancellation token you can use to stop the processing. This will stop more tasks being enqued, but will allow tasks already started to complete.</param>
        /// <typeparam name="TIn">The type of input values you will supply.</typeparam>
        /// <returns>A <see cref="IAsyncEnumerable"/> of <see cref="Result{TIn}"/> which has the input value and any exception that was encountered whilst running the action.</returns>
        public static async IAsyncEnumerable<Result<TIn>> SafeParrallelWithResult<TIn>(this IEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (inputValues is null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var taskQueue = new Queue<Task<Result<TIn>>>();

            var sem = new SemaphoreSlim(maxParallelism);
            foreach (var input in inputValues)
            {
                await sem.WaitAsync();

                taskQueue.Enqueue(RunIt(input, action, sem));

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

        /// <summary>
        /// Runs the action over all the input items and returns the output.
        /// Errors will be caught and returned in the Result object.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with
        /// streaming IEnumerables that you are reading from an external source.
        /// Ensure you iterate through the results as they are being processed! If you don't consume the results, the processing will stop.
                /// </summary>
        /// <param name="inputValues">The IEnumerable you want actions carried out on.</param>
        /// <param name="action">The async action you want carried out on each item in inputValues.</param>
        /// <param name="maxParallelism">How many tasks should be allowed to run concurrently.</param>
        /// <param name="cancellationToken">An optional cancellation token you can use to stop the processing. This will stop more tasks being enqued, but will allow tasks already started to complete.</param>
        /// <typeparam name="TIn">The type of input values you will supply.</typeparam>
        /// <typeparam name="TOut">The type of the output from your action.</typeparam>
        /// <returns>A <see cref="IAsyncEnumerable"/> of <see cref="Result{TIn}"/> which has the input value and any exception that was encountered whilst running the action.</returns>
        public static async IAsyncEnumerable<Result<TIn, TOut>> SafeParrallelWithResult<TIn, TOut>(this IEnumerable<TIn> inputValues, Func<TIn, Task<TOut>> action, int maxParallelism = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (inputValues is null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var taskQueue = new Queue<Task<Result<TIn, TOut>>>();

            var sem = new SemaphoreSlim(maxParallelism);
            foreach (var input in inputValues)
            {
                await sem.WaitAsync();

                taskQueue.Enqueue(RunIt(input, action, sem));

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