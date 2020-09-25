namespace AdvancedUse
{
    public interface IQueueWriter
    {
        Task Write(string message);
    }

    public class FakeQueueWriter : IQueueWriter
    {
        public async Task Write(string message)
        {
            Console.Write("Writing {0} to queue", message);
            await Task.Delay(50);            
        }
    }
}