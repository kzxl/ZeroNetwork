using System;

namespace ZeroNetwork.Http
{
    /// <summary>
    /// Configuration options for <see cref="ZeroApiClient"/>.
    /// </summary>
    public class ZeroApiClientOptions
    {
        /// <summary>
        /// Base URI for all relative API requests (e.g. "https://api.erp.local/").
        /// </summary>
        public string? BaseAddress { get; set; }

        /// <summary>
        /// Global request timeout (default: 30 seconds).
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum retry attempts for transient network failures (default: 3).
        /// Set to 0 to disable retry.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Initial delay before first retry (default: 200ms, scales exponentially).
        /// </summary>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Default Bearer Token added to the Authorization header of every request.
        /// </summary>
        public string? DefaultBearerToken { get; set; }

        /// <summary>
        /// Default API Key added to the 'x-api-key' header of every request.
        /// </summary>
        public string? DefaultApiKey { get; set; }

        /// <summary>
        /// Enables automatic socket-level GZip, Deflate, and Brotli decompression (default: true).
        /// Saves 70-80% of network transmission bandwidth.
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// How long a pooled TCP connection is kept alive before refreshing DNS (default: 15 minutes).
        /// Prevents DNS staleness issues when servers change IP or failover.
        /// </summary>
        public TimeSpan PooledConnectionLifetime { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Maximum idle time before an unused connection is closed (default: 2 minutes).
        /// </summary>
        public TimeSpan PooledConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Maximum concurrent TCP connections allowed per destination endpoint (default: 50).
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = 50;

        /// <summary>
        /// Custom JSON serializer. If null, the default serializer will be used.
        /// </summary>
        public IZeroJsonSerializer? Serializer { get; set; }
    }
}
