namespace SafeParallelAsync.Tests
{
    using System;
    using SafeParallelAsync;
    using Shouldly;
    using Xunit;

    public class ResultTests
    {
        private readonly object input = new object();

        [Fact]
        public void ExposesCorrectInput()
        {
            var sud = new Result<object>(this.input);
            sud.Input.ShouldBeSameAs(this.input);
        }

        [Fact]
        public void ResultReportsSuccess()
        {
            var sud = new Result<object>(this.input);
            sud.Success.ShouldBeTrue();
        }

        [Fact]
        public void HandlesErrorMessage()
        {
            var sud = new Result<object>(this.input, "error");
            sud.ErrorMessage.ShouldBe("error");
            sud.Success.ShouldBeFalse();
        }

        [Fact]
        public void HandlesException()
        {
            var ex = new Exception("exception error");
            var sud = new Result<object>(this.input, ex);
            sud.Success.ShouldBeFalse();
            sud.ErrorMessage.ShouldBe(ex.Message);
        }
    }
}