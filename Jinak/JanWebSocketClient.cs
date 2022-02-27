using System.Net.WebSockets;

namespace Jinak;

public class JanWebSocketClient
{
    public string Url { get; }
    public byte[] Buffer { get; set; }
    public ClientWebSocket Client { get; private set; }
    private CancellationTokenSource? ReadLoopCancellation { get; set; }
    public Action<WebSocketReceiveResult>? OnMessage { get; set; }
    public Action<Exception>? OnReadException { get; set; }

    public JanWebSocketClient(string url, uint bufferSize = 64 * 1024)
    {
        Url = url;
        Buffer = new byte[bufferSize];
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (ReadLoopCancellation != null)
        {
            throw new InvalidOperationException("Already connected");
        }

        Client = new();
        await Client.ConnectAsync(new Uri(Url), cancellationToken);
        ReadLoopCancellation = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            while (true)
            {
                ReadLoopCancellation.Token.ThrowIfCancellationRequested();
                try
                {
                    var result = await Client.ReceiveAsync(new(Buffer), ReadLoopCancellation.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _disconnected();
                        break;
                    }
                    OnMessage?.Invoke(result);
                }
                catch (Exception exc)
                {
                    _disconnected();
                    OnReadException?.Invoke(exc);
                }
            }
        }, ReadLoopCancellation.Token);
    }

    private void _disconnected()
    {
        ReadLoopCancellation = null;
    }

    public async Task DisconnectAsync()
    {
        ReadLoopCancellation?.Cancel();
        await Client.CloseAsync(WebSocketCloseStatus.Empty, "", default);
    }
}