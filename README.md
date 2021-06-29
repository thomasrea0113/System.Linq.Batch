## System.Linq.Batch

A set of concurrency extensions for running asynchronous actions in parallel.

### Usage

With `IAsyncEnumerable<>`:

```csharp

async IAsyncEnumerable<int> BatchAction(IEnumerable<int> items, CancellationToken token)
{
    var count = items.Count();
    foreach (var item in items)
    {
        token.ThrowIfCancellationRequested();
        await Task.Delay(BatchActionSleep / count, token).ConfigureAwait(false);
        yield return item;
    }
}

var results = Enumerable.Repeat(0, 100)
    .BatchActionAsync(10, BatchAction, TimeSpan.FromMilliseconds(BatchActionSleep * 10), default)
    .ToArrayAsync()

```

Extensions are also provided for operating directly on an instance of `IAsyncEnumerable<>`, and for batch actions that return an `IAsyncEnumerable<>`, or simply a ValueTask (for batches that produce no output)