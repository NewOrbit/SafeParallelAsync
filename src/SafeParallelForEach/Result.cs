namespace SafeParallelForEach
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    public class Result<TIn>
    {
        private string? errorMessage;

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

        public TIn Input { get; set; }

        public bool Success { get => this.ErrorMessage is null; }

        public string? ErrorMessage
        {
            get => this.errorMessage ?? this.Exception?.Message;
            private set => this.errorMessage = value;
        }

        public Exception? Exception { get; private set; }
    }

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "It's a lonely child")]
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

        [MaybeNull] // See https://stackoverflow.com/questions/55975211/nullable-reference-types-how-to-specify-t-type-without-constraining-to-class
        [AllowNull]
        public TOut Output { get; private set; } = default(TOut);
    }
}