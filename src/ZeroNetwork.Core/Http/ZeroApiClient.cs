using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroNetwork.Http
{
    /// <summary>
    /// Ultra-high performance, stream-based HTTP API Client.
    /// Eliminates intermediate string allocations and Large Object Heap (LOH) fragmentation by reading
    /// and writing payloads directly to and from network streams.
    /// Features built-in enterprise resilience, connection pooling, and socket compression.
    /// </summary>
    public class ZeroApiClient : IDisposable
    {
        private static IZeroJsonSerializer _defaultSerializer =
#if NET8_0_OR_GREATER
            new SystemTextJsonStreamSerializer();
#else
            new FallbackStreamSerializer();
#endif

        /// <summary>
        /// Gets or sets the global default JSON serializer used across all ZeroApiClient instances.
        /// </summary>
        public static IZeroJsonSerializer DefaultSerializer
        {
            get => _defaultSerializer;
            set => _defaultSerializer = value ?? throw new ArgumentNullException(nameof(value));
        }

        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;
        private readonly ZeroApiClientOptions _options;
        private readonly IZeroJsonSerializer _serializer;
        private bool _disposed;

        public ZeroApiClientOptions Options => _options;
        public IZeroJsonSerializer Serializer => _serializer;

        public ZeroApiClient(ZeroApiClientOptions? options = null)
        {
            _options = options ?? new ZeroApiClientOptions();
            _serializer = _options.Serializer ?? DefaultSerializer;

            HttpMessageHandler handler = CreateOptimalHandler(_options);
            _httpClient = new HttpClient(handler, disposeHandler: true);

            if (!string.IsNullOrWhiteSpace(_options.BaseAddress))
            {
                _httpClient.BaseAddress = new Uri(_options.BaseAddress!.TrimEnd('/') + "/");
            }

            _httpClient.Timeout = _options.Timeout;
            _ownsClient = true;
        }

        public ZeroApiClient(string baseAddress) : this(new ZeroApiClientOptions { BaseAddress = baseAddress })
        {
        }

        public ZeroApiClient(HttpClient httpClient, IZeroJsonSerializer? serializer = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = new ZeroApiClientOptions();
            _serializer = serializer ?? DefaultSerializer;
            _ownsClient = false;
        }

        private static HttpMessageHandler CreateOptimalHandler(ZeroApiClientOptions options)
        {
#if NET8_0_OR_GREATER
            return new SocketsHttpHandler
            {
                PooledConnectionLifetime = options.PooledConnectionLifetime,
                PooledConnectionIdleTimeout = options.PooledConnectionIdleTimeout,
                EnableMultipleHttp2Connections = true,
                MaxConnectionsPerServer = options.MaxConnectionsPerServer,
                AutomaticDecompression = options.EnableCompression
                    ? DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
                    : DecompressionMethods.None
            };
#else
            var handler = new HttpClientHandler();
            if (options.EnableCompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            return handler;
#endif
        }

        /// <summary>
        /// Sends an HTTP GET request and streams response directly into <typeparamref name="T"/> without intermediate string allocations.
        /// </summary>
        public Task<T?> GetAsync<T>(
            string uri,
            object? queryParams = null,
            string? bearerToken = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            string fullUri = AppendQueryParams(uri, queryParams);
            return SendAndDeserializeAsync<T>(
                () => CreateRequest(HttpMethod.Get, fullUri, null, bearerToken, apiKey),
                cancellationToken);
        }

        /// <summary>
        /// Sends an HTTP POST request with a JSON payload and streams response directly into <typeparamref name="TResult"/>.
        /// </summary>
        public async Task<TResult?> PostAsync<TRequest, TResult>(
            string uri,
            TRequest payload,
            string? bearerToken = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            var contentStream = new MemoryStream();
            await _serializer.SerializeAsync(contentStream, payload, cancellationToken).ConfigureAwait(false);
            byte[] payloadBytes = contentStream.ToArray();

            return await SendAndDeserializeAsync<TResult>(
                () =>
                {
                    var content = new ByteArrayContent(payloadBytes);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                    return CreateRequest(HttpMethod.Post, uri, content, bearerToken, apiKey);
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an HTTP PUT request with a JSON payload and streams response directly into <typeparamref name="TResult"/>.
        /// </summary>
        public async Task<TResult?> PutAsync<TRequest, TResult>(
            string uri,
            TRequest payload,
            string? bearerToken = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            var contentStream = new MemoryStream();
            await _serializer.SerializeAsync(contentStream, payload, cancellationToken).ConfigureAwait(false);
            byte[] payloadBytes = contentStream.ToArray();

            return await SendAndDeserializeAsync<TResult>(
                () =>
                {
                    var content = new ByteArrayContent(payloadBytes);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                    return CreateRequest(HttpMethod.Put, uri, content, bearerToken, apiKey);
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an HTTP DELETE request and streams response directly into <typeparamref name="TResult"/>.
        /// </summary>
        public Task<TResult?> DeleteAsync<TResult>(
            string uri,
            string? bearerToken = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            return SendAndDeserializeAsync<TResult>(
                () => CreateRequest(HttpMethod.Delete, uri, null, bearerToken, apiKey),
                cancellationToken);
        }

        /// <summary>
        /// Sends an HTTP GET request and returns the raw response stream.
        /// Caller is responsible for disposing the returned Stream.
        /// </summary>
        public async Task<Stream> GetStreamAsync(
            string uri,
            string? bearerToken = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            var response = await ExecuteWithRetryAsync(
                () => CreateRequest(HttpMethod.Get, uri, null, bearerToken, apiKey),
                cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        private async Task<T?> SendAndDeserializeAsync<T>(
            Func<HttpRequestMessage> requestFactory,
            CancellationToken cancellationToken)
        {
            using (var response = await ExecuteWithRetryAsync(requestFactory, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return await _serializer.DeserializeAsync<T>(stream, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private HttpRequestMessage CreateRequest(
            HttpMethod method,
            string uri,
            HttpContent? content,
            string? bearerToken,
            string? apiKey)
        {
            var request = new HttpRequestMessage(method, uri);
            if (content != null)
            {
                request.Content = content;
            }

            string? token = !string.IsNullOrEmpty(bearerToken) ? bearerToken : _options.DefaultBearerToken;
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            string? key = !string.IsNullOrEmpty(apiKey) ? apiKey : _options.DefaultApiKey;
            if (!string.IsNullOrEmpty(key))
            {
                request.Headers.Add("x-api-key", key);
            }

            return request;
        }

        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
            Func<HttpRequestMessage> requestFactory,
            CancellationToken cancellationToken)
        {
            int attempts = 0;
            int maxAttempts = Math.Max(1, _options.MaxRetryAttempts + 1);
            TimeSpan delay = _options.InitialRetryDelay;

            while (true)
            {
                attempts++;
                cancellationToken.ThrowIfCancellationRequested();

                HttpRequestMessage request = requestFactory();
                HttpResponseMessage? response = null;
                Exception? caughtException = null;

                try
                {
                    response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsTransientException(ex, cancellationToken))
                {
                    caughtException = ex;
                }

                if (response != null && !IsTransientStatusCode(response.StatusCode))
                {
                    return response;
                }

                if (attempts >= maxAttempts)
                {
                    if (response != null) return response;
                    if (caughtException != null) throw caughtException;
                }

                response?.Dispose();

                // Exponential backoff with jitter
                int jitterMs = new Random().Next(10, 50);
                await Task.Delay(delay + TimeSpan.FromMilliseconds(jitterMs), cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }

        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == (HttpStatusCode)429 || // TooManyRequests
                   statusCode == HttpStatusCode.BadGateway ||
                   statusCode == HttpStatusCode.ServiceUnavailable ||
                   statusCode == HttpStatusCode.GatewayTimeout;
        }

        private static bool IsTransientException(Exception ex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return false;
            return ex is HttpRequestException || ex is SocketException || ex is IOException || ex is TimeoutException;
        }

        /// <summary>
        /// Appends an object's properties or dictionary key-values to a URL as a query string.
        /// </summary>
        public static string AppendQueryParams(string url, object? queryParams)
        {
            if (queryParams == null) return url;

            var sb = new StringBuilder(url);
            bool hasQuery = url.IndexOf('?') >= 0;

            if (queryParams is IDictionary dict)
            {
                foreach (var key in dict.Keys)
                {
                    var val = dict[key];
                    if (val == null) continue;

                    sb.Append(hasQuery ? '&' : '?');
                    hasQuery = true;
                    sb.Append(Uri.EscapeDataString(key.ToString()!));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(val.ToString()!));
                }
                return sb.ToString();
            }

            PropertyInfo[] props = queryParams.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                if (p.GetIndexParameters().Length > 0) continue;

                object? val = p.GetValue(queryParams, null);
                if (val == null) continue;

                sb.Append(hasQuery ? '&' : '?');
                hasQuery = true;
                sb.Append(Uri.EscapeDataString(p.Name));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(val.ToString()!));
            }

            return sb.ToString();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_ownsClient)
            {
                _httpClient.Dispose();
            }
        }
    }
}
