using System.Threading;
using System.Threading.Tasks;
using Xunit;

using System.Diagnostics;
using System.Collections.Generic;

namespace System.Linq.Batch.Tests
{
    public class TestNoReturnBatchActions
    {
        /// <summary>
        /// A margin of error for the elapsed time of tasks to account for system processing time.
        /// If tests start failing intermittently, adjusting these 2 values should help
        /// </summary>
        public const int SystemProcessingDelay = 250;
        public const int BatchActionSleep = 1000;

        private static readonly IEnumerable<int> _testSequence = Enumerable.Repeat(0, 100);

        private static async ValueTask BatchAction(IEnumerable<int> _, CancellationToken token)
        {
            await Task.Delay(BatchActionSleep, token).ConfigureAwait(false);
        }

        // TODO fails intermittently when running all tests
        [Fact]
        public async Task TestCompleted()
        {
            using var cancelSource = new CancellationTokenSource();
            var stopwatch = Stopwatch.StartNew();
            await _testSequence.BatchActionAsync(10, BatchAction, TimeSpan.FromMilliseconds(BatchActionSleep * 10), cancelSource.Token).ConfigureAwait(false);
            stopwatch.Stop();
            Assert.InRange(stopwatch.ElapsedMilliseconds, BatchActionSleep, BatchActionSleep + SystemProcessingDelay);
        }

        [Fact]
        public async Task TestTimeout()
        {
            using var cancelSource = new CancellationTokenSource(SystemProcessingDelay * 2);
            var batchAction = _testSequence.BatchActionAsync(10, BatchAction, TimeSpan.FromMilliseconds(SystemProcessingDelay), cancelSource.Token);
            // we canceled after the timeout occured, so the we should get a timeout exception
            await Assert.ThrowsAsync<TimeoutException>(async () => await batchAction.ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestCancel()
        {
            using var cancelSource = new CancellationTokenSource(10);
            var batchAction = _testSequence.BatchActionAsync(10, BatchAction, TimeSpan.FromMilliseconds(BatchActionSleep + SystemProcessingDelay), cancelSource.Token);

            // we canceled after half of the timeout and task sleep time, so the cancellation should have occurred first
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await batchAction.ConfigureAwait(false)).ConfigureAwait(false);
        }

        // TODO fails intermittently when running all tests
        [Fact]
        public async Task TestCancelledInActionBeforeTimeout()
        {
            using var cancelSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(BatchActionSleep / 2));
            var batchAction = _testSequence.BatchActionAsync(10, async (items, t) =>
            {
                await Task.Delay(BatchActionSleep / 2 + SystemProcessingDelay, t).ConfigureAwait(false); // need to sleep at least the time of the delay
            }, TimeSpan.FromMilliseconds(BatchActionSleep), cancelSource.Token);

            // here, a task cancelled exception is thrown because it's not our code that actually raises the exception
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await batchAction.ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestCancelledInActionAfterTimeout()
        {
            using var cancelSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(BatchActionSleep));
            var batchAction = _testSequence.BatchActionAsync(10, async (items, t) =>
            {
                await Task.Delay(BatchActionSleep / 2 + SystemProcessingDelay, t).ConfigureAwait(false); // need to sleep at least the time of the timeout
                t.ThrowIfCancellationRequested();
            }, TimeSpan.FromMilliseconds(BatchActionSleep / 2), cancelSource.Token);
            await Assert.ThrowsAsync<TimeoutException>(async () => await batchAction.ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}
