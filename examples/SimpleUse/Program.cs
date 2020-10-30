using System;
using System.Linq;
using System.Threading.Tasks;
using SafeParallelAsync;

namespace SimpleUse
{
    class Program
    {
        static void Main(string[] args)
        {
            Test().Wait();
            
        }

        private static async Task Test()
        {
            IQueueWriter queueWriter = new FakeQueueWriter();
            var enumerable = Enumerable.Range(1, 100).Select(i => $"Messsage {i}");

            await enumerable.SafeParallel(async msg => await queueWriter.Write(msg));
        }
    }


}
