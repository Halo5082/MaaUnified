using System.Net;
using System.Text;

namespace MAAUnified.Application.Services.WebApi;

internal sealed class WebApiServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly Func<HttpListenerContext, CancellationToken, Task> _handler;
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenTask;

    public WebApiServer(string host, int port, Func<HttpListenerContext, CancellationToken, Task> handler)
    {
        _handler = handler;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://{host}:{port}/");
    }

    public void Start()
    {
        if (_listenTask is not null)
        {
            return;
        }

        _listener.Start();
        _listenTask = Task.Run(AcceptLoopAsync, CancellationToken.None);
    }

    public async Task StopAsync()
    {
        if (_listenTask is null)
        {
            return;
        }

        _cts.Cancel();
        _listener.Close();
        try
        {
            await _listenTask.ConfigureAwait(false);
        }
        catch (HttpListenerException)
        {
            // Listener closed while stopping.
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cts.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                continue;
            }

            _ = Task.Run(() => _handler(context!, _cts.Token), _cts.Token);
        }
    }
}
