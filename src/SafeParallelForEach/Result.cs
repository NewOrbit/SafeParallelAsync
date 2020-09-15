using System;

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
            set => errorMessage = value; 
            }
        public Exception? Exception { get; set; }
    }

}