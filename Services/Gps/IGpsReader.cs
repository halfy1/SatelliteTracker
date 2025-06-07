public interface IGpsReader
{
    Task StartAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken);
}
