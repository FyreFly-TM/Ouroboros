using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ouroboros.StdLib.Net
{
    /// <summary>
    /// TCP listener for creating server applications
    /// </summary>
    public class TcpListener : IDisposable
    {
        private readonly global::System.Net.Sockets.TcpListener listener;
        private readonly List<TcpConnection> connections = new();
        private CancellationTokenSource? cancellationTokenSource;
        private bool isListening = false;
        private bool disposed = false;

        public event EventHandler<TcpConnection>? ClientConnected;
        public event EventHandler<Exception>? ErrorOccurred;

        public TcpListener(int port)
        {
            listener = new global::System.Net.Sockets.TcpListener(IPAddress.Any, port);
        }

        public TcpListener(string address, int port)
        {
            var ipAddress = IPAddress.Parse(address);
            listener = new global::System.Net.Sockets.TcpListener(ipAddress, port);
        }

        /// <summary>
        /// Start listening for connections
        /// </summary>
        public void Start(int backlog = 10)
        {
            if (isListening)
                throw new InvalidOperationException("Already listening");

            listener.Start(backlog);
            isListening = true;
            cancellationTokenSource = new CancellationTokenSource();

            // Start accepting connections
            Task.Run(() => AcceptConnectionsAsync(cancellationTokenSource.Token));
        }

        /// <summary>
        /// Stop listening for connections
        /// </summary>
        public void Stop()
        {
            if (!isListening)
                return;

            isListening = false;
            cancellationTokenSource?.Cancel();
            listener.Stop();

            // Close all active connections
            foreach (var connection in connections.ToArray())
            {
                connection.Close();
            }
            connections.Clear();
        }

        /// <summary>
        /// Accept a single connection synchronously
        /// </summary>
        public TcpConnection AcceptConnection()
        {
            if (!isListening)
                throw new InvalidOperationException("Not listening");

            var tcpClient = listener.AcceptTcpClient();
            var connection = new TcpConnection(tcpClient);
            connections.Add(connection);
            return connection;
        }

        /// <summary>
        /// Accept a single connection asynchronously
        /// </summary>
        public async Task<TcpConnection> AcceptConnectionAsync()
        {
            if (!isListening)
                throw new InvalidOperationException("Not listening");

            var tcpClient = await listener.AcceptTcpClientAsync();
            var connection = new TcpConnection(tcpClient);
            connections.Add(connection);
            return connection;
        }

        private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    var connection = new TcpConnection(tcpClient);
                    connections.Add(connection);

                    // Notify listeners
                    ClientConnected?.Invoke(this, connection);
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped
                    break;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Stop();
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    /// TCP connection wrapper
    /// </summary>
    public class TcpConnection : IDisposable
    {
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private StreamReader? reader;
        private StreamWriter? writer;
        private bool disposed = false;

        public bool Connected => client.Connected;
        public EndPoint? RemoteEndPoint => client.Client.RemoteEndPoint;
        public EndPoint? LocalEndPoint => client.Client.LocalEndPoint;

        internal TcpConnection(TcpClient client)
        {
            this.client = client;
            this.stream = client.GetStream();
        }

        public TcpConnection(string host, int port)
        {
            client = new TcpClient();
            client.Connect(host, port);
            stream = client.GetStream();
        }

        public async Task ConnectAsync(string host, int port)
        {
            await client.ConnectAsync(host, port);
        }

        /// <summary>
        /// Send raw bytes
        /// </summary>
        public void Send(byte[] data)
        {
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        /// <summary>
        /// Send raw bytes asynchronously
        /// </summary>
        public async Task SendAsync(byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        /// <summary>
        /// Send text
        /// </summary>
        public void SendText(string text)
        {
            writer ??= new StreamWriter(stream) { AutoFlush = true };
            writer.WriteLine(text);
        }

        /// <summary>
        /// Send text asynchronously
        /// </summary>
        public async Task SendTextAsync(string text)
        {
            writer ??= new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync(text);
        }

        /// <summary>
        /// Receive raw bytes
        /// </summary>
        public int Receive(byte[] buffer)
        {
            return stream.Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Receive raw bytes asynchronously
        /// </summary>
        public async Task<int> ReceiveAsync(byte[] buffer)
        {
            return await stream.ReadAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Receive text line
        /// </summary>
        public string? ReceiveText()
        {
            reader ??= new StreamReader(stream);
            return reader.ReadLine();
        }

        /// <summary>
        /// Receive text line asynchronously
        /// </summary>
        public async Task<string?> ReceiveTextAsync()
        {
            reader ??= new StreamReader(stream);
            return await reader.ReadLineAsync();
        }

        /// <summary>
        /// Set socket options
        /// </summary>
        public void SetSocketOption(SocketOptionLevel level, SocketOptionName name, object value)
        {
            client.Client.SetSocketOption(level, name, value);
        }

        /// <summary>
        /// Set timeouts
        /// </summary>
        public void SetTimeout(int receiveTimeoutMs, int sendTimeoutMs)
        {
            client.ReceiveTimeout = receiveTimeoutMs;
            client.SendTimeout = sendTimeoutMs;
        }

        /// <summary>
        /// Enable keep-alive
        /// </summary>
        public void EnableKeepAlive(int interval = 1000, int time = 1000)
        {
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            
            // TCP keep-alive settings (platform-specific)
            byte[] keepAlive = new byte[12];
            BitConverter.GetBytes(1).CopyTo(keepAlive, 0); // on/off
            BitConverter.GetBytes(time).CopyTo(keepAlive, 4); // time
            BitConverter.GetBytes(interval).CopyTo(keepAlive, 8); // interval
            
            client.Client.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
        }

        /// <summary>
        /// Close the connection
        /// </summary>
        public void Close()
        {
            reader?.Close();
            writer?.Close();
            stream?.Close();
            client?.Close();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Close();
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    /// UDP socket for connectionless communication
    /// </summary>
    public class UdpSocket : IDisposable
    {
        private readonly UdpClient udpClient;
        private bool disposed = false;

        public UdpSocket(int port = 0)
        {
            udpClient = new UdpClient(port);
        }

        /// <summary>
        /// Send datagram
        /// </summary>
        public int Send(byte[] data, string host, int port)
        {
            return udpClient.Send(data, data.Length, host, port);
        }

        /// <summary>
        /// Send datagram asynchronously
        /// </summary>
        public async Task<int> SendAsync(byte[] data, string host, int port)
        {
            return await udpClient.SendAsync(data, data.Length, host, port);
        }

        /// <summary>
        /// Receive datagram
        /// </summary>
        public UdpReceiveResult Receive()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            var data = udpClient.Receive(ref remoteEP);
            return new UdpReceiveResult(data, remoteEP);
        }

        /// <summary>
        /// Receive datagram asynchronously
        /// </summary>
        public async Task<UdpReceiveResult> ReceiveAsync()
        {
            return await udpClient.ReceiveAsync();
        }

        /// <summary>
        /// Join multicast group
        /// </summary>
        public void JoinMulticastGroup(string multicastAddress)
        {
            var address = IPAddress.Parse(multicastAddress);
            udpClient.JoinMulticastGroup(address);
        }

        /// <summary>
        /// Leave multicast group
        /// </summary>
        public void LeaveMulticastGroup(string multicastAddress)
        {
            var address = IPAddress.Parse(multicastAddress);
            udpClient.DropMulticastGroup(address);
        }

        public void Close()
        {
            udpClient?.Close();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Close();
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
} 