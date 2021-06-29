using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Batch.Tests
{
    public class TestTaskTimeoutAfterNoReturn
    {
        static Task Do(int delay, double timeout, CancellationToken token)
            => Task.Delay(delay, token).TimeoutAfter(TimeSpan.FromMilliseconds(timeout), token);

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
            await Do(150, 300, cancelToken.Token).ConfigureAwait(false);
        }
    }
}
