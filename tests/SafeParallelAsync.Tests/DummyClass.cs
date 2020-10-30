namespace SafeParallelAsync.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    internal class DummyClass
    {
        public DummyClass(int value)
        {
            this.MyInt = value;
        }

        public int MyInt { get; set; }
    }
}
