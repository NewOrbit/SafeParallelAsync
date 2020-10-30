using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SafeParallel;

namespace AdvancedUse
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to start");
            Console.ReadKey();
            Test().Wait();
            Console.WriteLine("Done. Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task Test()
        {
            var idList = Enumerable.Range(1, 1000000);
            IDatabaseReader dbReader = new FakeDatabaseReader(500);
            IDatabaseWriter dbWriter = new FakeDatabaseWriter(200);
            IQueueWriter queueWriter = new FakeQueueWriter(25);

            // Note that IAsyncEnumerable has a nifty "WithCancellation" method, which you can just call at the end and 
            // it will apply down through the stack. However, you can only do this when the result is an IAsyncEnumerable.
            // In this example, I  
            
            var cancellationToken = new CancellationToken();

            var process = idList.SafeParallelAsyncWithResult(id => dbReader.ReadData(id), 30)
                          .SafeParallelAsyncWithResult(readResult => dbWriter.WriteData(readResult.Output), 100)
                          .SafeParallelAsync(writeResult => queueWriter.Write(writeResult.Output.SomeDescription), 50, cancellationToken);

            await process;


            

        }
    }
}
