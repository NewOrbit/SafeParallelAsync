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
        int minWriteTime;

        public FakeDatabaseWriter(int minWriteTime)
        {
            this.minWriteTime = minWriteTime;    
        }
        private static Random rnd = new Random();
        
        public async Task<PostWriteData> WriteData(SourceData data)
        {
            Console.Write("Writing record {0} to the other database", data.SomeDescription);
            
            await Task.Delay(this.minWriteTime + rnd.Next(10));            

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