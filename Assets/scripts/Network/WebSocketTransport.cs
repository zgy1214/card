using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public sealed class WebSocketTransport
{
    private readonly ConcurrentQueue<string> _receivedRawMessages = new ConcurrentQueue<string>();
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

    private ClientWebSocket _clientWebSocket;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _receiveLoopTask;

    public bool IsConnected
    {
        get
        {
            return _clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open;
        }
    }

    public bool IsConnecting { get; private set; }

    public async Task ConnectAsync(string serverUri)
    {
        if (string.IsNullOrEmpty(serverUri))
        {
            throw new ArgumentException("Server URI cannot be null or empty.", nameof(serverUri));
        }

        if (IsConnected || IsConnecting)
        {
            return;
        }

        Disconnect();

        ClientWebSocket newClientWebSocket = new ClientWebSocket();
        CancellationTokenSource newCancellationTokenSource = new CancellationTokenSource();
        IsConnecting = true;
        try
        {
            await newClientWebSocket.ConnectAsync(new Uri(serverUri), newCancellationTokenSource.Token);
            _clientWebSocket = newClientWebSocket;
            _cancellationTokenSource = newCancellationTokenSource;
            _receiveLoopTask = ReceiveLoopAsync(_clientWebSocket, _cancellationTokenSource.Token);
        }
        catch
        {
            newCancellationTokenSource.Cancel();
            newCancellationTokenSource.Dispose();
            newClientWebSocket.Dispose();
            throw;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    public async Task SendTextAsync(string rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
        {
            throw new ArgumentException("Raw message cannot be null or empty.", nameof(rawMessage));
        }

        if (!IsConnected)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }

        byte[] rawMessageBytes = Encoding.UTF8.GetBytes(rawMessage);
        await _sendLock.WaitAsync();
        try
        {
            await _clientWebSocket.SendAsync(
                new ArraySegment<byte>(rawMessageBytes),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public bool TryDequeueReceivedMessage(out string rawMessage)
    {
        return _receivedRawMessages.TryDequeue(out rawMessage);
    }

    public void Disconnect()
    {
        ClientWebSocket currentClientWebSocket = _clientWebSocket;
        CancellationTokenSource currentCancellationTokenSource = _cancellationTokenSource;
        _clientWebSocket = null;
        _cancellationTokenSource = null;
        _receiveLoopTask = null;
        IsConnecting = false;

        if (currentCancellationTokenSource != null)
        {
            try
            {
                currentCancellationTokenSource.Cancel();
            }
            catch
            {
            }
        }

        if (currentClientWebSocket != null)
        {
            try
            {
                currentClientWebSocket.Abort();
            }
            catch
            {
            }

            currentClientWebSocket.Dispose();
        }

        currentCancellationTokenSource?.Dispose();
        while (_receivedRawMessages.TryDequeue(out _))
        {
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket clientWebSocket, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (!cancellationToken.IsCancellationRequested
                && clientWebSocket.State == WebSocketState.Open)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        receiveResult = await clientWebSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            cancellationToken);

                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }

                        memoryStream.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);

                    string rawMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                    _receivedRawMessages.Enqueue(rawMessage);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (WebSocketException)
        {
        }
    }
}
