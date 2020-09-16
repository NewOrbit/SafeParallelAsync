using System;
using System.Diagnostics.CodeAnalysis;

namespace SafeParallelForEach
{
    public class Result<TIn>
    {
        public Result(TIn input)
        {
            this.Input = input;
        }

        public Result(TIn input, string errorMessage) : this(input)
        {
            this.errorMessage = errorMessage;
        }

        public Result(TIn input, Exception exception) : this(input)
        {
            this.Exception = exception;
        }

        private string? errorMessage;

        public TIn Input { get; set; }

        public bool Success { get => this.ErrorMessage is null; }

        public string? ErrorMessage { 
            get => errorMessage ?? this.Exception?.Message; 
            private set => errorMessage = value; 
            }
        public Exception? Exception { get; private set; }
    }

    public class Result<TIn, TOut> : Result<TIn>
    {
        public Result(TIn input, TOut output) : base(input)
        {
            this.Output = output;
        }

        public Result(TIn input, string errorMessage) : base(input, errorMessage)
        {
        }

        public Result(TIn input, Exception exception) : base(input, exception)
        {
        }

        [MaybeNull, AllowNull] // See https://stackoverflow.com/questions/55975211/nullable-reference-types-how-to-specify-t-type-without-constraining-to-class
        public TOut Output { get; private set; } = default(TOut);
    }
    

}