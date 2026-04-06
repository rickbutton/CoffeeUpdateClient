using System.IO;
using System.IO.Pipes;
using Serilog;

namespace CoffeeUpdater.Utils;

public class SingleInstanceGuard : IDisposable
{
    private const string MutexName = "Global\\CoffeeUpdater";
    private const string PipeName = "CoffeeUpdater";
    private const string ShowCommand = "SHOW";

    private Mutex? _mutex;
    private CancellationTokenSource? _pipeCts;

    public event Action? ShowRequested;

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            return false;
        }
        return true;
    }

    public static void SignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeout: 3000);
            using var writer = new StreamWriter(client);
            writer.WriteLine(ShowCommand);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to signal existing instance via named pipe");
        }
    }

    public void StartListening()
    {
        _pipeCts = new CancellationTokenSource();
        _ = ListenForCommandsAsync(_pipeCts.Token);
    }

    private async Task ListenForCommandsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct);

                using (server)
                using (var reader = new StreamReader(server))
                {
                    var command = await reader.ReadLineAsync(ct);
                    if (command == ShowCommand)
                    {
                        Log.Information("Received SHOW command from another instance");
                        ShowRequested?.Invoke();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in named pipe listener");
            }
        }
    }

    public void Dispose()
    {
        _pipeCts?.Cancel();
        _pipeCts?.Dispose();
        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}
