namespace AdvancedUse
{
    using System;
    using System.Threading.Tasks;

    public interface IDatabaseWriter
    {
        Task<PostWriteData> WriteData(SourceData data);
    }

    public class FakeDatabaseWriter : IDatabaseWriter
    {
        private int minWriteTime;
        
        private static Random rnd = new Random();

        public FakeDatabaseWriter(int minWriteTime)
        {
            this.minWriteTime = minWriteTime;    
        }
        
        
        public async Task<PostWriteData> WriteData(SourceData data)
        {
            Console.WriteLine("Writing record {0} to the other database", data.SomeDescription);
            
            await Task.Delay(this.minWriteTime + rnd.Next(100));            

            return new PostWriteData(data);
        }
    }

    public class PostWriteData
    {
        public PostWriteData(SourceData sourceData)
        {
            this.Id = sourceData.Id;
        }
        public int Id { get; set; }

        public string SomeDescription { get => $"Record {this.Id}";  }
    }

}