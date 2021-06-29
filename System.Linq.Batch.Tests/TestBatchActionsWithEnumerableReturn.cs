using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Batch.Tests
{
    public class TestBatchActionsWithEnumerableReturn
    {
        /// <summary>
        /// A margin of error for the elapsed time of tasks to account for system processing time.
        /// If tests start failing intermittently, adjusting these 2 values should help
        /// </summary>
        public const int SystemProcessingDelay = 250;
        public const int BatchActionSleep = 1000;

        private static readonly IEnumerable<int> _testSequence = Enumerable.Repeat(0, 100);

        private IEnumerable<int> BatchAction(IEnumerable<int> items, CancellationToken token)
        {
            Task.Delay(BatchActionSleep, token).GetAwaiter().GetResult();
            foreach (var item in items)
                yield return item;
        }

        // TODO fails intermittently when running all tests
        [Fact]
        public async Task TestCompleted()
        {
            using var cancelSource = new CancellationTokenSource();
            var stopwatch = Stopwatch.StartNew();
            var results = await _testSequence.BatchActionAsync(10, BatchAction, TimeSpan.FromMilliseconds(BatchActionSleep * 10), cancelSource.Token)
                                                 .ToArrayAsync().ConfigureAwait(false);
            stopwatch.Stop();
            Assert.InRange(stopwatch.ElapsedMilliseconds, BatchActionSleep, BatchActionSleep + SystemProcessingDelay);
            Assert.Equal(_testSequence, results);
        }

        [Fact]
        public async Task TestTimeout()
        {
            using var cancelSource = new CancellationTokenSource(SystemProcessingDelay * 2);
            var batchAction = _testSequence.BatchActionAsync(10, BatchAction, TimeSpan.FromMilliseconds(SystemProcessingDelay), cancelSource.Token).ToArrayAsync();
            // we canceled after the timeout occured, so the we should get a timeout exception
            await Assert.ThrowsAsync<TimeoutException>(async () => await batchAction.ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestCancel()
        {
            using var cancelSource = new CancellationTokenSource(10);
            var batchAction = _testSequence.BatchActionAsync(10, BatchAction, TimeSpan.FromMilliseconds(BatchActionSleep + SystemProcessingDelay), cancelSource.Token).ToArrayAsync();
            
            // we canceled after half of the timeout and task sleep time, so the cancellation should have occurred first
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await batchAction.ConfigureAwait(false)).ConfigureAwait(false);
        }

        // TODO fails intermittently when running all tests
        [Fact]
        public async Task TestCancelledInActionBeforeTimeout()
        {
            static IEnumerable<int> Act(IEnumerable<int> items, CancellationToken token)
            {
                Task.Delay(BatchActionSleep / 2 + SystemProcessingDelay, token).GetAwaiter().GetResult(); // need to sleep at least the time of the delay
                foreach (var item in items)
                    yield return item;
            }

            using var cancelSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(BatchActionSleep / 2));
            var batchAction = _testSequence.BatchActionAsync(10, Act, TimeSpan.FromMilliseconds(BatchActionSleep), cancelSource.Token).ToArrayAsync();

            // here, a task cancelled exception is thrown because it's not our code that actually raises the exception
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await batchAction.ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestCancelledInActionAfterTimeout()
        {
            static IEnumerable<int> Act(IEnumerable<int> items, CancellationToken token)
            {
                Task.Delay(BatchActionSleep / 2 + SystemProcessingDelay, token).GetAwaiter().GetResult(); // need to sleep at least the time of the timeout
                token.ThrowIfCancellationRequested();
                foreach (var item in items)
                    yield return item;
            }

            using var cancelSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(BatchActionSleep));
            var batchAction = _testSequence.BatchActionAsync(10, Act, TimeSpan.FromMilliseconds(BatchActionSleep / 2), cancelSource.Token).ToArrayAsync();
            await Assert.ThrowsAsync<TimeoutException>(async () => await batchAction.ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}
