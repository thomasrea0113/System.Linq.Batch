using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace System.Linq.Batch
{
    public static class IEnumerableBatchExtensions
    {
        /// <summary>
        /// Pretty handy function that performs the given action on each item in the enumerable. A task
        /// will be scheduled for each batch. The action is performed once on each batch.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="items"></param>
        /// <param name="batchSize"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async static IAsyncEnumerable<TResult> BatchActionAsync<T, TResult>(
            this IEnumerable<T> items,
            int batchSize,
            Func<IEnumerable<T>, CancellationToken, ValueTask<IEnumerable<TResult>>> action,
            TimeSpan timeout,
            CancellationToken token)
        {
            // create a new token source, and ensure that it is canceled after the given delay
            using var timeoutCancelSource = new CancellationTokenSource(timeout);
            using var taskCancelSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancelSource.Token, token);

            var tasks = items.Split(batchSize)
                             .Select(batch => Task.Run(async () => await action(batch, taskCancelSource.Token).ConfigureAwait(false), taskCancelSource.Token))
                             .ToArray(); // ToArrray() enumerates the tasks, ensuring they're all started

            foreach (var batchTask in tasks)
            {
                TResult[] batchResults;
                try
                {
                    taskCancelSource.Token.ThrowIfCancellationRequested();
                    batchResults = (await batchTask.ConfigureAwait(false)).ToArray();
                }
                catch (TaskCanceledException e)
                {
                    if (timeoutCancelSource.IsCancellationRequested)
                        throw new TimeoutException("Timeout", e);
                    throw;
                }

                foreach (var batchResult in batchResults)
                    yield return batchResult;
            }
        }

        public static IAsyncEnumerable<TResult> BatchActionAsync<T, TResult>(
            this IEnumerable<T> items,
            int batchSize,
            Func<IEnumerable<T>, CancellationToken, IAsyncEnumerable<TResult>> action,
            TimeSpan timeout,
            CancellationToken token)
        {
            async ValueTask<IEnumerable<TResult>> TaskFunc(IEnumerable<T> batch, CancellationToken token)
                => await action(batch, token).ToArrayAsync(token).ConfigureAwait(false);

            return items.BatchActionAsync(batchSize, TaskFunc, timeout, token);
        }

        public static IAsyncEnumerable<TResult> BatchActionAsync<T, TResult>(
            this IEnumerable<T> items,
            int batchSize,
            Func<IEnumerable<T>, CancellationToken, IEnumerable<TResult>> action,
            TimeSpan timeout,
            CancellationToken token)
        {
            // need to enumerate the internal array. This is less efficient if action already returns an array,
            // because it will re-enumerate the result.
            async ValueTask<IEnumerable<TResult>> TaskFunc(IEnumerable<T> batch, CancellationToken token)
                => await Task.Run(() => action(batch, token).ToArray()).ConfigureAwait(false);

            return items.BatchActionAsync(batchSize, TaskFunc, timeout, token);
        }

        /// <summary>
        /// Same as <see cref="BatchActionAsync{T, TResult}(IEnumerable{T}, int, Func{T, TResult})"/>, except action does not return a result.
        /// When the type of the action is not an IEnumerable, the action will be performed on the batch, not the batch.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="batchSize"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// Task.WaitAll(messages.BatchActionAsync(5, (IEnumerable<MessageResource> msg) => {}));
        /// </code>
        /// </example>
        public static async Task BatchActionAsync<T>(
            this IEnumerable<T> items,
            int batchSize,
            Func<IEnumerable<T>, CancellationToken, ValueTask> action,
            TimeSpan timeout,
            CancellationToken token)
        {
            // create a new token source, and ensure that it is canceled after the given delay
            using var timeoutCancelSource = new CancellationTokenSource(timeout);
            using var taskCancelSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancelSource.Token, token);

            var tasks = items.Split(batchSize)
                             .Select(batch => Task.Run(async () => await action(batch, taskCancelSource.Token).ConfigureAwait(false), taskCancelSource.Token))
                             .ToArray(); // ToArrray() enumerates the tasks, ensuring they're all started

            foreach (var batchTask in tasks)
            {
                try
                {
                    taskCancelSource.Token.ThrowIfCancellationRequested();
                    await batchTask.ConfigureAwait(false);
                }
                catch (TaskCanceledException e)
                {
                    if (timeoutCancelSource.IsCancellationRequested)
                        throw new TimeoutException("Timeout", e);
                    throw;
                }
            }
        }
    }
}
