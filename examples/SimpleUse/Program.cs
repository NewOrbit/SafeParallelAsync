using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SafeParallel;

namespace SimpleUse
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowThreadingProblem().Wait();
            
        }

        private static async Task Test()
        {
            IQueueWriter queueWriter = new FakeQueueWriter();
            var enumerable = Enumerable.Range(1, 100).Select(i => $"Messsage {i}");

            await enumerable.SafeParallelAsync(async msg => await queueWriter.Write(msg));
        }

        private static async Task Test2()
        {
            IQueueWriter queueWriter = new FakeQueueWriter();
            var enumerable = Enumerable.Range(1, 100).Select(i => $"Messsage {i}");
            var cancellationToken = new CancellationToken();

            await enumerable.SafeParallelAsync(async msg => await queueWriter.Write(msg), cancellationToken: cancellationToken);
        }

        private static async Task ShowThreadingProblem()
        {
            ThreadPool.SetMinThreads(100,100); 
            var enumerable = Enumerable.Range(1, 10000);
            int written = 0;

            await enumerable.SafeParallelAsync(async number => 
                {
                    await Task.Delay(1);
                    written++;
                    if (number % 1000 == 0)
                    {
                        Console.WriteLine($"Input: {number}. Written: {written}");
                    }
                    
                });

            Console.WriteLine(written);
        }
    }


}
