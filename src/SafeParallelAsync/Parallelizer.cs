namespace SafeParallel
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
        /// <param name="inputValues">The enumerable that has the values to be passed to the action.</param>
        /// <param name="action">The action you want to perfom on each item in the enumerable.</param>
        /// <param name="maxParallelism">The maximum number of tasks to run in parallel.</param>
        /// <param name="cancellationToken">A cancellation token you can use to stop the processing. If cancelled, the already-enqueed tasks will still be awaited but no more tasks will be enqued. This will not throw a <see cref="TaskCancelledException" />.</param>
        /// <typeparam name="TIn">The type of the values in inputValues.</typeparam>
        /// <returns>A <see cref="Task"/> you should await - when it's done, all the items have been processed.</returns>
        public static async Task SafeParallelAsync<TIn>(this IEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
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

                while (taskQueue.TryPeek(out var t) && t.IsCompleted)
                {
                    await taskQueue.Dequeue();
                }
            }

            await Task.WhenAll(taskQueue);
        }

        /// <summary>
        /// Runs the action over all the input items.
        /// There is no error handling - if the action throws an exception, it will blow and stop processing eventually.
        /// If desired you can put error handling in the action.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with
        /// streaming IEnumerables that you are reading from an external source.
        /// </summary>
        /// <param name="inputValues">The async enumerable that has the values to be passed to the action.</param>
        /// <param name="action">The action you want to perfom on each item in the enumerable.</param>
        /// <param name="maxParallelism">The maximum number of tasks to run in parallel.</param>
        /// <param name="cancellationToken">A cancellation token you can use to stop the processing. If cancelled, the already-enqueed tasks will still be awaited but no more tasks will be enqued. This will not throw a <see cref="TaskCancelledException" />.</param>
        /// <typeparam name="TIn">The type of the values in inputValues.</typeparam>
        /// <returns>A <see cref="Task"/> you should await - when it's done, all the items have been processed.</returns>
        public static async Task SafeParallelAsync<TIn>(this IAsyncEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
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
             await foreach (var input in inputValues.WithCancellation(cancellationToken))
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

                while (taskQueue.TryPeek(out var t) && t.IsCompleted)
                {
                    await taskQueue.Dequeue();
                }
             }

             await Task.WhenAll(taskQueue);
        }

        /// <summary>
        /// Runs the action over all the input items.
        /// Exceptions are caught and stored on the result object.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with
        /// streaming IEnumerables that you are reading from an external source.
        /// </summary>
        /// <param name="inputValues">The enumerable that has the values to be passed to the action.</param>
        /// <param name="action">The action you want to perfom on each item in the enumerable.</param>
        /// <param name="maxParallelism">The maximum number of tasks to run in parallel.</param>
        /// <param name="cancellationToken">A cancellation token you can use to stop the processing. If cancelled, the already-enqueed tasks will still be awaited but no more tasks will be enqued. This will not throw a <see cref="TaskCancelledException" />.</param>
        /// <typeparam name="TIn">The type of the values in inputValues.</typeparam>
        /// <returns>An IAsyncEnumerable with a <see cref="Result{TIn}"/> that has the input value and any exception.</returns>
        public static IAsyncEnumerable<Result<TIn>> SafeParallelAsyncWithResult<TIn>(this IEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return SafeParallelAsyncWithResult(inputValues, (TIn input, SemaphoreSlim sem) => RunIt(input, action, sem), maxParallelism, cancellationToken);
        }

        /// <summary>
        /// Runs the action over all the input items.
        /// Exceptions are caught and stored on the result object.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with
        /// streaming IEnumerables that you are reading from an external source.
        /// </summary>
        /// <param name="inputValues">The enumerable that has the values to be passed to the action.</param>
        /// <param name="action">The action you want to perfom on each item in the enumerable.</param>
        /// <param name="maxParallelism">The maximum number of tasks to run in parallel.</param>
        /// <param name="cancellationToken">A cancellation token you can use to stop the processing. If cancelled, the already-enqueed tasks will still be awaited but no more tasks will be enqued. This will not throw a <see cref="TaskCancelledException" />.</param>
        /// <typeparam name="TIn">The type of the values in inputValues.</typeparam>
        /// <typeparam name="TOut">The type of result from the action.</typeparam>
        /// <returns>An IAsyncEnumerable with a <see cref="Result{TIn, TOut}"/> that has the input value, the result and any exception.</returns>
        public static IAsyncEnumerable<Result<TIn, TOut>> SafeParallelAsyncWithResult<TIn, TOut>(this IEnumerable<TIn> inputValues, Func<TIn, Task<TOut>> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return SafeParallelAsyncWithResult(inputValues, (TIn input, SemaphoreSlim sem) => RunIt(input, action, sem), maxParallelism, cancellationToken);
        }

        /// <summary>
        /// Runs the action over all the input values.
        /// Exceptions are caught and stored on the result object.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with
        /// streaming IEnumerables that you are reading from an external source.
        /// </summary>
        /// <param name="inputValues">The async enumerable that has the values to be passed to the action.</param>
        /// <param name="action">The action you want to perfom on each item in the enumerable.</param>
        /// <param name="maxParallelism">The maximum number of tasks to run in parallel.</param>
        /// <param name="cancellationToken">A cancellation token you can use to stop the processing. If cancelled, the already-enqueed tasks will still be awaited but no more tasks will be enqued. This will not throw a <see cref="TaskCancelledException" />.</param>
        /// <typeparam name="TIn">The type of the values in inputValues.</typeparam>
        /// <returns>An IAsyncEnumerable with a <see cref="Result{TIn}"/> that has the input value and any exception.</returns>
        public static IAsyncEnumerable<Result<TIn>> SafeParallelAsyncWithResult<TIn>(this IAsyncEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return SafeParallelAsyncWithResult(inputValues, (TIn input, SemaphoreSlim sem) => RunIt(input, action, sem), maxParallelism, cancellationToken);
        }

        /// <summary>
        /// Runs the action over all the input values.
        /// Exceptions are caught and stored on the result object.
        /// This will only keep a relatively small number of tasks and input values in scope so can safely be used with
        /// streaming IEnumerables that you are reading from an external source.
        /// </summary>
        /// <param name="inputValues">The async enumerable that has the values to be passed to the action.</param>
        /// <param name="action">The action you want to perfom on each item in the enumerable.</param>
        /// <param name="maxParallelism">The maximum number of tasks to run in parallel.</param>
        /// <param name="cancellationToken">A cancellation token you can use to stop the processing. If cancelled, the already-enqueed tasks will still be awaited but no more tasks will be enqued. This will not throw a <see cref="TaskCancelledException" />.</param>
        /// <typeparam name="TIn">The type of the values in inputValues.</typeparam>
        /// <typeparam name="TOut">The type of the return value from the action.</typeparam>
        /// <returns>An IAsyncEnumerable with a <see cref="Result{TIn, TOut}"/> that has the input value, the result and any exception.</returns>
        public static IAsyncEnumerable<Result<TIn, TOut>> SafeParallelAsyncWithResult<TIn, TOut>(this IAsyncEnumerable<TIn> inputValues, Func<TIn, Task<TOut>> action, int maxParallelism = 100, CancellationToken cancellationToken = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return SafeParallelAsyncWithResult(inputValues, (TIn input, SemaphoreSlim sem) => RunIt(input, action, sem), maxParallelism, cancellationToken);
        }

        private static async IAsyncEnumerable<TResult> SafeParallelAsyncWithResult<TIn, TResult>(IEnumerable<TIn> inputValues, Func<TIn, SemaphoreSlim, Task<TResult>> runner, int maxParallelism = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                while (taskQueue.TryPeek(out var t) && t.IsCompleted)
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

        private static async IAsyncEnumerable<TResult> SafeParallelAsyncWithResult<TIn, TResult>(IAsyncEnumerable<TIn> inputValues, Func<TIn, SemaphoreSlim, Task<TResult>> runner, int maxParallelism = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (inputValues is null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            var taskQueue = new Queue<Task<TResult>>();

            var sem = new SemaphoreSlim(maxParallelism);
            await foreach (var input in inputValues.WithCancellation(cancellationToken))
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
                while (taskQueue.TryPeek(out var t) && t.IsCompleted)
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