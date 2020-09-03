namespace SafeParallelForEach
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Parallelizer
    {
        public static async Task SafeParallel<Tin>(this IEnumerable<Tin> inputValues, Func<Tin, Task> action, int maxParallelism = 100)
        {

            var tl = new List<Task>();
            var sem = new SemaphoreSlim(maxParallelism);
            foreach(var input in inputValues) 
            {
                await sem.WaitAsync();
                tl.Add(action(input).ContinueWith(t => sem.Release()));
            }
            await Task.WhenAll(tl);

        }

        // Can I use IAsyncEnumerable to somehow stream the results back???
        // Action<Task> callback = null ??
    }
}