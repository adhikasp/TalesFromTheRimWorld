using System;

namespace AINarrator
{
    /// <summary>
    /// Result of an HTTP request to the OpenRouter API.
    /// </summary>
    public class HttpTransportResult
    {
        public bool Success { get; set; }
        public string ResponseBody { get; set; }
        public string Error { get; set; }
        public int StatusCode { get; set; }
        
        public static HttpTransportResult Ok(string body) => new HttpTransportResult 
        { 
            Success = true, 
            ResponseBody = body,
            StatusCode = 200
        };
        
        public static HttpTransportResult Fail(string error, int statusCode = 0) => new HttpTransportResult 
        { 
            Success = false, 
            Error = error,
            StatusCode = statusCode
        };
    }
    
    /// <summary>
    /// Configuration for HTTP transport.
    /// Abstracts settings access so the transport doesn't depend on ModSettings directly.
    /// </summary>
    public class HttpTransportConfig
    {
        public string ApiKey { get; set; }
        public string ApiUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
        public int TimeoutSeconds { get; set; } = 60;
        public string Referer { get; set; } = "https://rimworld-ainarrator.local";
        public string Title { get; set; } = "Tales from the RimWorld";
    }
    
    /// <summary>
    /// Interface for HTTP transport layer.
    /// Allows swapping between Unity coroutines (production) and synchronous requests (testing).
    /// </summary>
    public interface IHttpTransport
    {
        /// <summary>
        /// Send a POST request with JSON body.
        /// For async transports (Unity), callbacks are invoked when complete.
        /// For sync transports (testing), callbacks are invoked immediately after the request completes.
        /// </summary>
        /// <param name="jsonBody">JSON request body</param>
        /// <param name="config">Transport configuration</param>
        /// <param name="onComplete">Callback with result</param>
        void PostJson(string jsonBody, HttpTransportConfig config, Action<HttpTransportResult> onComplete);
    }
}

