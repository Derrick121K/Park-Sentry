namespace ParkSentry.IntegrationTests.Infrastructure;

public static class AsyncWait
{
    public static async Task<T> WaitForAsync<T>(
        TaskCompletionSource<T> source,
        TimeSpan timeout,
        string failureMessage)
    {
        using var cts = new CancellationTokenSource(timeout);
        await using var registration = cts.Token.Register(() =>
            source.TrySetException(new TimeoutException(failureMessage)));

        return await source.Task;
    }

    public static async Task EnsureNotCompletedAsync<T>(
        TaskCompletionSource<T> source,
        TimeSpan quietPeriod,
        string failureMessage)
    {
        var completed = await Task.WhenAny(source.Task, Task.Delay(quietPeriod));
        if (ReferenceEquals(completed, source.Task))
            throw new InvalidOperationException(failureMessage);
    }
}
