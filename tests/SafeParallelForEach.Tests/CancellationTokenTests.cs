namespace SafeParallelForEach.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class CancellationTokenTests
    {
        private CancellationToken token;

        [Fact]
        public async Task CancellationTokensBehaveAsExpected()
        {
            this.token = new CancellationToken(canceled: true); // Start cancelled to show it makes no difference if my code doesn't check it
            this.token.ShouldNotBe(default);
            var enumerable = this.GetAsync();
            int count = 0;
            await foreach (var i in enumerable.WithCancellation(this.token))
            {
                count++;
            }

            count.ShouldBe(100, "The enumerable returned early....");
        }

        private async IAsyncEnumerable<int> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ShouldBe(this.token, "I do not understand how the compiler rewrites this part - mind blown");
            foreach (var i in Enumerable.Range(1, 100))
            {
                cancellationToken.ShouldBe(this.token, "The cancellation token inside the loop is not the magically passed one");
                await Task.Delay(1);
                yield return i;
            }
        }
    }
}
