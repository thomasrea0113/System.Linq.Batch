using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Batch.Tests
{
    public class TestTaskTimeoutAfterWithReturn
    {
        static Task<int> Do(int delay, double timeout, CancellationToken token)
        {
            async Task<int> Act()
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
                return 0;
            }
                
            return Act().TimeoutAfter(TimeSpan.FromMilliseconds(timeout), token);
        }

        [Fact]
        public async Task TestCancelledBeforeTimeoutWithReturn()
        {
            using var cancelToken = new CancellationTokenSource(25);
            await Assert.ThrowsAsync<TaskCanceledException>(() => Do(75, 100, cancelToken.Token)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestCancelledAfterTimeoutWithReturn()
        {
            using var cancelToken = new CancellationTokenSource(450);
            // the delay and timeout need to be decently spread, because the timeout takes some time to process. In that time,
            // the task may have finished
            await Assert.ThrowsAsync<TimeoutException>(() => Do(500, 100, cancelToken.Token)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestCompletedBeforeCancelOrTimeoutWithReturn()
        {
            using var cancelToken = new CancellationTokenSource(450);
            var result = await Do(150, 300, cancelToken.Token).ConfigureAwait(false);
            Assert.Equal(0, result);
        }
    }
}
