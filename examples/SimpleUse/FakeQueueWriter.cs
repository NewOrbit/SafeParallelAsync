using System;
using System.Threading.Tasks;

namespace SimpleUse
{
    public interface IQueueWriter
    {
        Task Write(string message);
    }

    public class FakeQueueWriter : IQueueWriter
    {
        public async Task Write(string message)
        {
            Console.WriteLine("Writing {0} to queue", message);
            await Task.Delay(50);            
        }
    }
}