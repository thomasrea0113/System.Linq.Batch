using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Batch
{
    public static class TaskExtensions
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            // a token source that will timeout at the specified interval, or if cancelled outside of this scope
            using var timeoutTokenSource = new CancellationTokenSource(timeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

            async Task Do()
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException e)
                {
                    if (timeoutTokenSource.IsCancellationRequested)
                        throw new TimeoutException("Timeout", e);
                    throw;
                }
            }

            await Task.Run(Do, linkedTokenSource.Token).ConfigureAwait(false);
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            // a token source that will timeout at the specified interval, or if cancelled outside of this scope
            using var timeoutTokenSource = new CancellationTokenSource(timeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

            async Task<T> Do()
            {
                try
                {
                    return await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException e)
                {
                    if (timeoutTokenSource.IsCancellationRequested)
                        throw new TimeoutException("Timeout", e);
                    throw;
                }
            }

            return await Task.Run(Do, linkedTokenSource.Token).ConfigureAwait(false);
        }
    }
}
