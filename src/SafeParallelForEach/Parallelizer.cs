namespace SafeParallelForEach
{
    using System;
    using System.Collections.Generic;
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
        public static async Task SafeParallel<TIn>(this IEnumerable<TIn> inputValues, Func<TIn, Task> action, int maxParallelism = 100)
        {
            if (inputValues is null)
            {
                throw new ArgumentNullException(nameof(inputValues));
            }

            var taskQueue = new Queue<Task>();

            // var tl = new List<Task>();
            var sem = new SemaphoreSlim(maxParallelism);
            foreach (var input in inputValues)
            {
                await sem.WaitAsync();
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

        private static async Task RunIt(Task task, SemaphoreSlim sem)
        {
            await task;
            sem.Release();
        }

        // Can I use IAsyncEnumerable to somehow stream the results back???
        // Action<Task> callback = null ??
        // exception handling
    }
}