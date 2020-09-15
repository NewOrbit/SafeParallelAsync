namespace SafeParallelForEach.Tests
{
    using System;
    using SafeParallelForEach;
    using Shouldly;
    using Xunit;

    public class ResultTests
    {
        object input = new object();

        [Fact]
        public void ExposesCorrectInput()
        {
            var sud = new Result<object>(input);
            sud.Input.ShouldBeSameAs(input);
        }

        [Fact]
        public void ResultReportsSuccess()
        {
            var sud = new Result<object>(input);
            sud.Success.ShouldBeTrue();
        }

        [Fact]
        public void HandlesErrorMessage()
        {
            var sud = new Result<object>(input, "error");
            sud.ErrorMessage.ShouldBe("error");
            sud.Success.ShouldBeFalse();
        }

        [Fact]
        public void HandlesException()
        {
            var ex = new Exception("exception error");
            var sud = new Result<object>(input,ex);
            sud.Success.ShouldBeFalse();
            sud.ErrorMessage.ShouldBe(ex.Message);
        }
    }
}