using System;
using System.Threading.Tasks;

namespace AdvancedUse
{
    public interface IQueueWriter
    {
        Task Write(string message);
    }

    public class FakeQueueWriter : IQueueWriter
    {
        int minWriteTime;
        private static Random rnd = new Random();

        public FakeQueueWriter(int minWriteTime)
        {
            this.minWriteTime = minWriteTime;
            
        }
        public async Task Write(string message)
        {
            Console.WriteLine("Writing {0} to queue", message);
            await Task.Delay(minWriteTime + rnd.Next(100));
        }
    }
}