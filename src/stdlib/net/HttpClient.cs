using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Ouroboros.StdLib.Net
{
    /// <summary>
    /// HTTP client implementation for Ouroboros
    /// </summary>
    public class HttpClient : IDisposable
    {
        private readonly int timeout;
        private readonly Dictionary<string, string> defaultHeaders;
        private bool disposed = false;

        public HttpClient(int timeoutMs = 30000)
        {
            this.timeout = timeoutMs;
            this.defaultHeaders = new Dictionary<string, string>
            {
                ["User-Agent"] = "Ouroboros/1.0",
                ["Accept"] = "*/*",
                ["Accept-Encoding"] = "gzip, deflate",
                ["Connection"] = "keep-alive"
            };
        }

        /// <summary>
        /// Send GET request
        /// </summary>
        public async Task<HttpResponse> GetAsync(string url)
        {
            return await SendRequestAsync("GET", url, null, null);
        }

        /// <summary>
        /// Send POST request
        /// </summary>
        public async Task<HttpResponse> PostAsync(string url, string content, string contentType = "application/json")
        {
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = contentType,
                ["Content-Length"] = Encoding.UTF8.GetByteCount(content).ToString()
            };
            return await SendRequestAsync("POST", url, content, headers);
        }

        /// <summary>
        /// Send PUT request
        /// </summary>
        public async Task<HttpResponse> PutAsync(string url, string content, string contentType = "application/json")
        {
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = contentType,
                ["Content-Length"] = Encoding.UTF8.GetByteCount(content).ToString()
            };
            return await SendRequestAsync("PUT", url, content, headers);
        }

        /// <summary>
        /// Send DELETE request
        /// </summary>
        public async Task<HttpResponse> DeleteAsync(string url)
        {
            return await SendRequestAsync("DELETE", url, null, null);
        }

        /// <summary>
        /// Send custom HTTP request
        /// </summary>
        public async Task<HttpResponse> SendRequestAsync(string method, string url, string? body, Dictionary<string, string>? headers)
        {
            var uri = new Uri(url);
            var isHttps = uri.Scheme == "https";
            var port = uri.Port == -1 ? (isHttps ? 443 : 80) : uri.Port;

            using var client = new TcpClient();
            client.ReceiveTimeout = timeout;
            client.SendTimeout = timeout;

            await client.ConnectAsync(uri.Host, port);

            Stream stream = client.GetStream();
            
            if (isHttps)
            {
                // For HTTPS, we'd need SSL/TLS support
                // For now, use System.Net.Http for HTTPS
                return await SendHttpsRequestAsync(method, url, body, headers);
            }

            // Build HTTP request
            var request = new StringBuilder();
            request.AppendLine($"{method} {uri.PathAndQuery} HTTP/1.1");
            request.AppendLine($"Host: {uri.Host}");

            // Add default headers
            foreach (var header in defaultHeaders)
            {
                request.AppendLine($"{header.Key}: {header.Value}");
            }

            // Add custom headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.AppendLine($"{header.Key}: {header.Value}");
                }
            }

            request.AppendLine(); // Empty line before body

            // Add body if present
            if (!string.IsNullOrEmpty(body))
            {
                request.Append(body);
            }

            // Send request
            var requestBytes = Encoding.UTF8.GetBytes(request.ToString());
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
            await stream.FlushAsync();

            // Read response
            return await ReadResponseAsync(stream);
        }

        private async Task<HttpResponse> ReadResponseAsync(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            
            // Read status line
            var statusLine = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(statusLine))
            {
                throw new HttpException("Empty response from server");
            }

            var statusParts = statusLine.Split(' ', 3);
            if (statusParts.Length < 3)
            {
                throw new HttpException($"Invalid status line: {statusLine}");
            }

            var response = new HttpResponse
            {
                StatusCode = int.Parse(statusParts[1]),
                StatusText = statusParts[2],
                Headers = new Dictionary<string, string>()
            };

            // Read headers
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var name = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();
                    response.Headers[name] = value;
                }
            }

            // Read body
            if (response.Headers.TryGetValue("Content-Length", out var contentLengthStr))
            {
                var contentLength = int.Parse(contentLengthStr);
                var buffer = new char[contentLength];
                await reader.ReadAsync(buffer, 0, contentLength);
                response.Body = new string(buffer);
            }
            else if (response.Headers.TryGetValue("Transfer-Encoding", out var encoding) && 
                     encoding.Contains("chunked"))
            {
                response.Body = await ReadChunkedBodyAsync(reader);
            }
            else
            {
                // Read until end
                response.Body = await reader.ReadToEndAsync();
            }

            return response;
        }

        private async Task<string> ReadChunkedBodyAsync(StreamReader reader)
        {
            var body = new StringBuilder();
            
            while (true)
            {
                var chunkSizeLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(chunkSizeLine))
                    break;

                var chunkSize = Convert.ToInt32(chunkSizeLine, 16);
                if (chunkSize == 0)
                    break;

                var buffer = new char[chunkSize];
                await reader.ReadAsync(buffer, 0, chunkSize);
                body.Append(buffer);

                // Read trailing CRLF
                await reader.ReadLineAsync();
            }

            return body.ToString();
        }

        private async Task<HttpResponse> SendHttpsRequestAsync(string method, string url, string? body, Dictionary<string, string>? headers)
        {
            // Fallback to System.Net.Http for HTTPS
            using var client = new global::System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeout);

            var request = new global::System.Net.Http.HttpRequestMessage(
                new global::System.Net.Http.HttpMethod(method), 
                url);

            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new global::System.Net.Http.StringContent(body, Encoding.UTF8);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await client.SendAsync(request);
            
            return new HttpResponse
            {
                StatusCode = (int)response.StatusCode,
                StatusText = response.ReasonPhrase ?? "",
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Body = await response.Content.ReadAsStringAsync()
            };
        }

        public void SetDefaultHeader(string name, string value)
        {
            defaultHeaders[name] = value;
        }

        public void RemoveDefaultHeader(string name)
        {
            defaultHeaders.Remove(name);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    /// HTTP response
    /// </summary>
    public class HttpResponse
    {
        public int StatusCode { get; set; }
        public string StatusText { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = "";

        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
        public bool IsRedirect => StatusCode >= 300 && StatusCode < 400;
        public bool IsClientError => StatusCode >= 400 && StatusCode < 500;
        public bool IsServerError => StatusCode >= 500 && StatusCode < 600;
    }

    /// <summary>
    /// HTTP exception
    /// </summary>
    public class HttpException : Exception
    {
        public HttpException(string message) : base(message) { }
        public HttpException(string message, Exception innerException) : base(message, innerException) { }
    }
} 