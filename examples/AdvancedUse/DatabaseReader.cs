namespace AdvancedUse
{
    using System;
    using System.Threading.Tasks;

    public interface IDatabaseReader
    {
        Task<SourceData> ReadData(int id);
    }

    public class FakeDatabaseReader : IDatabaseReader
    {
        int minReadTime;

        public FakeDatabaseReader(int minReadTime)
        {
            this.minReadTime = minReadTime;    
        }
        private static Random rnd = new Random();
        
        public async Task<SourceData> ReadData(int id)
        {
            Console.Write("Reading record {0} from the database", id.ToString());
            
            await Task.Delay(this.minReadTime + rnd.Next(10));            

            return new SourceData(id);
        }
    }

    public class SourceData
    {
        public SourceData(int id)
        {
            this.Id = id;
        }
        public int Id { get; set; }

        public string SomeDescription { get => $"Record {this.Id}";  }
    }
}