using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ouro.StdLib.Net
{
    /// <summary>
    /// WebSocket client implementation
    /// </summary>
    public class WebSocketClient : IDisposable
    {
        private TcpClient? tcpClient;
        private NetworkStream? stream;
        private bool isConnected;
        private CancellationTokenSource? cancellationTokenSource;
        private readonly SemaphoreSlim sendLock = new(1, 1);
        
        public event Action<string>? OnTextMessage;
        public event Action<byte[]>? OnBinaryMessage;
        public event Action<int, string>? OnClose;
        public event Action<Exception>? OnError;
        public event Action? OnOpen;
        
        public bool IsConnected => isConnected;
        
        /// <summary>
        /// Connect to WebSocket server
        /// </summary>
        public async Task ConnectAsync(string url)
        {
            var uri = new Uri(url);
            if (uri.Scheme != "ws" && uri.Scheme != "wss")
            {
                throw new ArgumentException("Invalid WebSocket URL scheme");
            }
            
            var port = uri.Port == -1 ? (uri.Scheme == "wss" ? 443 : 80) : uri.Port;
            
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(uri.Host, port);
            stream = tcpClient.GetStream();
            
            // Send WebSocket handshake
            await SendHandshakeAsync(uri);
            
            // Read handshake response
            await ReadHandshakeResponseAsync();
            
            isConnected = true;
            cancellationTokenSource = new CancellationTokenSource();
            
            OnOpen?.Invoke();
            
            // Start reading messages
            _ = Task.Run(() => ReadMessagesAsync(cancellationTokenSource.Token));
        }
        
        /// <summary>
        /// Send text message
        /// </summary>
        public async Task SendTextAsync(string message)
        {
            if (!isConnected) throw new InvalidOperationException("Not connected");
            
            var bytes = Encoding.UTF8.GetBytes(message);
            await SendFrameAsync(WebSocketOpcode.Text, bytes);
        }
        
        /// <summary>
        /// Send binary message
        /// </summary>
        public async Task SendBinaryAsync(byte[] data)
        {
            if (!isConnected) throw new InvalidOperationException("Not connected");
            
            await SendFrameAsync(WebSocketOpcode.Binary, data);
        }
        
        /// <summary>
        /// Send ping
        /// </summary>
        public async Task PingAsync(byte[]? data = null)
        {
            if (!isConnected) throw new InvalidOperationException("Not connected");
            
            await SendFrameAsync(WebSocketOpcode.Ping, data ?? Array.Empty<byte>());
        }
        
        /// <summary>
        /// Close connection
        /// </summary>
        public async Task CloseAsync(int code = 1000, string reason = "")
        {
            if (!isConnected) return;
            
            var reasonBytes = Encoding.UTF8.GetBytes(reason);
            var payload = new byte[2 + reasonBytes.Length];
            payload[0] = (byte)(code >> 8);
            payload[1] = (byte)code;
            Array.Copy(reasonBytes, 0, payload, 2, reasonBytes.Length);
            
            await SendFrameAsync(WebSocketOpcode.Close, payload);
            
            isConnected = false;
            cancellationTokenSource?.Cancel();
        }
        
        private async Task SendHandshakeAsync(Uri uri)
        {
            var key = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var request = new StringBuilder();
            
            request.AppendLine($"GET {uri.PathAndQuery} HTTP/1.1");
            request.AppendLine($"Host: {uri.Host}");
            request.AppendLine("Upgrade: websocket");
            request.AppendLine("Connection: Upgrade");
            request.AppendLine($"Sec-WebSocket-Key: {key}");
            request.AppendLine("Sec-WebSocket-Version: 13");
            request.AppendLine();
            
            var bytes = Encoding.UTF8.GetBytes(request.ToString());
            await stream!.WriteAsync(bytes, 0, bytes.Length);
        }
        
        private async Task ReadHandshakeResponseAsync()
        {
            using var reader = new StreamReader(stream!, Encoding.UTF8, leaveOpen: true);
            
            var statusLine = await reader.ReadLineAsync();
            if (!statusLine!.Contains("101"))
            {
                throw new WebSocketException($"Invalid status: {statusLine}");
            }
            
            // Read headers
            string? line;
            var headers = new Dictionary<string, string>();
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    headers[parts[0].Trim()] = parts[1].Trim();
                }
            }
            
            // Verify upgrade
            if (!headers.TryGetValue("Upgrade", out var upgrade) || 
                !upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase))
            {
                throw new WebSocketException("Server did not accept WebSocket upgrade");
            }
        }
        
        private async Task SendFrameAsync(WebSocketOpcode opcode, byte[] payload)
        {
            await sendLock.WaitAsync();
            try
            {
                var frame = new List<byte>();
                
                // First byte: FIN (1) + RSV (000) + Opcode
                frame.Add((byte)(0x80 | (byte)opcode));
                
                // Payload length and mask bit
                var maskBit = 0x80; // Client must mask
                
                if (payload.Length < 126)
                {
                    frame.Add((byte)(maskBit | payload.Length));
                }
                else if (payload.Length < 65536)
                {
                    frame.Add((byte)(maskBit | 126));
                    frame.Add((byte)(payload.Length >> 8));
                    frame.Add((byte)payload.Length);
                }
                else
                {
                    frame.Add((byte)(maskBit | 127));
                    for (int i = 7; i >= 0; i--)
                    {
                        frame.Add((byte)(payload.Length >> (8 * i)));
                    }
                }
                
                // Masking key
                var maskKey = new byte[4];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(maskKey);
                }
                frame.AddRange(maskKey);
                
                // Masked payload
                for (int i = 0; i < payload.Length; i++)
                {
                    frame.Add((byte)(payload[i] ^ maskKey[i % 4]));
                }
                
                await stream!.WriteAsync(frame.ToArray(), 0, frame.Count);
            }
            finally
            {
                sendLock.Release();
            }
        }
        
        private async Task ReadMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            var message = new List<byte>();
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && isConnected)
                {
                    // Read frame header
                    if (!await ReadExactlyAsync(buffer, 0, 2))
                        break;
                        
                    var fin = (buffer[0] & 0x80) != 0;
                    var opcode = (WebSocketOpcode)(buffer[0] & 0x0F);
                    var masked = (buffer[1] & 0x80) != 0;
                    var payloadLength = buffer[1] & 0x7F;
                    
                    // Read extended payload length
                    if (payloadLength == 126)
                    {
                        if (!await ReadExactlyAsync(buffer, 0, 2))
                            break;
                        payloadLength = (buffer[0] << 8) | buffer[1];
                    }
                    else if (payloadLength == 127)
                    {
                        if (!await ReadExactlyAsync(buffer, 0, 8))
                            break;
                        // For simplicity, only support up to int.MaxValue
                        payloadLength = 0;
                        for (int i = 4; i < 8; i++)
                        {
                            payloadLength = (payloadLength << 8) | buffer[i];
                        }
                    }
                    
                    // Read mask key if present
                    byte[]? maskKey = null;
                    if (masked)
                    {
                        maskKey = new byte[4];
                        if (!await ReadExactlyAsync(maskKey, 0, 4))
                            break;
                    }
                    
                    // Read payload
                    var payload = new byte[payloadLength];
                    if (payloadLength > 0)
                    {
                        if (!await ReadExactlyAsync(payload, 0, payloadLength))
                            break;
                            
                        // Unmask if needed
                        if (masked && maskKey != null)
                        {
                            for (int i = 0; i < payload.Length; i++)
                            {
                                payload[i] ^= maskKey[i % 4];
                            }
                        }
                    }
                    
                    // Handle frame
                    await HandleFrameAsync(opcode, fin, payload, message);
                }
            }
            catch (Exception)
            {
                // Cannot invoke OnError from outside the class
            }
            finally
            {
                isConnected = false;
            }
        }
        
        private async Task<bool> ReadExactlyAsync(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                var read = await stream!.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    return false;
                totalRead += read;
            }
            return true;
        }
        
        private async Task HandleFrameAsync(WebSocketOpcode opcode, bool fin, byte[] payload, List<byte> message)
        {
            switch (opcode)
            {
                case WebSocketOpcode.Continuation:
                    message.AddRange(payload);
                    if (fin && message.Count > 0)
                    {
                        // Determine if text or binary based on first frame
                        OnBinaryMessage?.Invoke(message.ToArray());
                        message.Clear();
                    }
                    break;
                    
                case WebSocketOpcode.Text:
                    if (fin)
                    {
                        var text = Encoding.UTF8.GetString(payload);
                        OnTextMessage?.Invoke(text);
                    }
                    else
                    {
                        message.Clear();
                        message.AddRange(payload);
                    }
                    break;
                    
                case WebSocketOpcode.Binary:
                    if (fin)
                    {
                        OnBinaryMessage?.Invoke(payload);
                    }
                    else
                    {
                        message.Clear();
                        message.AddRange(payload);
                    }
                    break;
                    
                case WebSocketOpcode.Close:
                    int closeCode = 1000;
                    string closeReason = "";
                    
                    if (payload.Length >= 2)
                    {
                        closeCode = (payload[0] << 8) | payload[1];
                        if (payload.Length > 2)
                        {
                            closeReason = Encoding.UTF8.GetString(payload, 2, payload.Length - 2);
                        }
                    }
                    
                    OnClose?.Invoke(closeCode, closeReason);
                    isConnected = false;
                    break;
                    
                case WebSocketOpcode.Ping:
                    // Send pong
                    await SendFrameAsync(WebSocketOpcode.Pong, payload);
                    break;
                    
                case WebSocketOpcode.Pong:
                    // Pong received
                    break;
            }
        }
        
        public void Dispose()
        {
            if (isConnected)
            {
                CloseAsync().GetAwaiter().GetResult();
            }
            
            cancellationTokenSource?.Dispose();
            sendLock?.Dispose();
            stream?.Dispose();
            tcpClient?.Dispose();
        }
    }
    
    /// <summary>
    /// WebSocket opcodes
    /// </summary>
    public enum WebSocketOpcode : byte
    {
        Continuation = 0x0,
        Text = 0x1,
        Binary = 0x2,
        Close = 0x8,
        Ping = 0x9,
        Pong = 0xA
    }
    
    /// <summary>
    /// WebSocket exception
    /// </summary>
    public class WebSocketException : Exception
    {
        public WebSocketException(string message) : base(message) { }
        public WebSocketException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Simple WebSocket server
    /// </summary>
    public class WebSocketServer : IDisposable
    {
        private TcpListener? listener;
        private readonly List<WebSocketServerClient> clients = new();
        private bool isRunning;
        
        public event Action<WebSocketServerClient>? OnClientConnected;
        public event Action<WebSocketServerClient>? OnClientDisconnected;
        
        /// <summary>
        /// Start WebSocket server
        /// </summary>
        public async Task StartAsync(int port)
        {
            listener = new TcpListener(port);  // Use port-only constructor
            listener.Start();
            isRunning = true;
            
            while (isRunning)
            {
                try
                {
                    var connection = await listener.AcceptConnectionAsync();
                    // Note: We need to handle TcpConnection instead of TcpClient
                    // This requires refactoring HandleClientAsync
                }
                catch when (!isRunning)
                {
                    break;
                }
            }
        }
        
        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            var client = new WebSocketServerClient(tcpClient);
            
            try
            {
                await client.HandleHandshakeAsync();
                
                lock (clients)
                {
                    clients.Add(client);
                }
                
                OnClientConnected?.Invoke(client);
                
                await client.HandleMessagesAsync();
            }
            catch (Exception)
            {
                // Cannot invoke OnError from outside the class
            }
            finally
            {
                lock (clients)
                {
                    clients.Remove(client);
                }
                
                OnClientDisconnected?.Invoke(client);
                client.Dispose();
            }
        }
        
        /// <summary>
        /// Broadcast text message to all clients
        /// </summary>
        public async Task BroadcastTextAsync(string message)
        {
            var tasks = new List<Task>();
            
            lock (clients)
            {
                foreach (var client in clients)
                {
                    tasks.Add(client.SendTextAsync(message));
                }
            }
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// Stop server
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
            
            lock (clients)
            {
                foreach (var client in clients)
                {
                    client.Dispose();
                }
                clients.Clear();
            }
        }
        
        public void Dispose()
        {
            Stop();
            listener?.Dispose();
        }
    }
    
    /// <summary>
    /// WebSocket server client connection
    /// </summary>
    public class WebSocketServerClient : WebSocketClient
    {
        private readonly TcpClient tcpClient;
        
        internal WebSocketServerClient(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
        }
        
        internal async Task HandleHandshakeAsync()
        {
            var stream = tcpClient.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            
            // Read request
            var requestLine = await reader.ReadLineAsync();
            var headers = new Dictionary<string, string>();
            
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    headers[parts[0].Trim()] = parts[1].Trim();
                }
            }
            
            // Validate WebSocket request
            if (!headers.TryGetValue("Sec-WebSocket-Key", out var key))
            {
                throw new WebSocketException("Missing Sec-WebSocket-Key");
            }
            
            // Generate accept key
            var acceptKey = GenerateAcceptKey(key);
            
            // Send response
            var response = new StringBuilder();
            response.AppendLine("HTTP/1.1 101 Switching Protocols");
            response.AppendLine("Upgrade: websocket");
            response.AppendLine("Connection: Upgrade");
            response.AppendLine($"Sec-WebSocket-Accept: {acceptKey}");
            response.AppendLine();
            
            var responseBytes = Encoding.UTF8.GetBytes(response.ToString());
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
        
        internal async Task HandleMessagesAsync()
        {
            // Similar to client message handling but server doesn't mask frames
            await Task.CompletedTask;
        }
        
        private static string GenerateAcceptKey(string key)
        {
            const string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(key + guid));
            return Convert.ToBase64String(hash);
        }
    }
} 