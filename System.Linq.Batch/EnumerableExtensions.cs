using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Batch
{
    /// <summary>
    /// A collection of general-purpose enumerable extensions
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Splits the enumerable into equal partitions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="paritionSize"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> sequence, int paritionSize)
        {
            while (sequence.Any())
            {
                var partition = sequence.Take(paritionSize);
                if (partition.Any())
                    yield return partition;
                sequence = sequence.Skip(paritionSize);
            }
        }
    }
    /// <summary>
    /// A collection of general-purpose enumerable extensions
    /// </summary>
    public static class IAsyncEnumerableExtensions
    {
        /// <summary>
        /// Splits the enumerable into equal partitions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="paritionSize"></param>
        /// <returns></returns>
        public async static IAsyncEnumerable<IAsyncEnumerable<T>> SplitAsync<T>(this IAsyncEnumerable<T> sequence, int paritionSize, CancellationToken token)
        {
            while (await sequence.AnyAsync(token).ConfigureAwait(false))
            {
                var partition = sequence.Take(paritionSize);
                if (await partition.AnyAsync(token).ConfigureAwait(false))
                    yield return partition;
                sequence = sequence.Skip(paritionSize);
            }
        }
    }
}
