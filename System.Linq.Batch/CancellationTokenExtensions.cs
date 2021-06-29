using System.Threading;

namespace System.Linq.Batch
{
    internal static class CancellationTokenExtensions
    {
        public static void ThrowTimeoutExceptionIfCancellationRequested(this CancellationToken timeoutCancelSource, Exception? innerException = default)
        {
            if (timeoutCancelSource.IsCancellationRequested)
                throw new TimeoutException("Task Timeout", innerException);
        }
    }
}
