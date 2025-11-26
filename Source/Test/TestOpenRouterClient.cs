using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

// Use actual DTOs from the main AINarrator project
using AINarrator;

namespace AINarrator.Test
{
    /// <summary>
    /// Synchronous HTTP transport for console testing.
    /// Uses WebRequest instead of Unity's coroutines.
    /// </summary>
    public class SyncHttpTransport : IHttpTransport
    {
        private readonly bool _debugMode;
        
        public SyncHttpTransport(bool debugMode = false)
        {
            _debugMode = debugMode;
        }
        
        /// <summary>
        /// Send a POST request with JSON body synchronously.
        /// The callback is invoked immediately after the request completes.
        /// </summary>
        public void PostJson(string jsonBody, HttpTransportConfig config, Action<HttpTransportResult> onComplete)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            var httpRequest = (HttpWebRequest)WebRequest.Create(config.ApiUrl);
            httpRequest.Method = "POST";
            httpRequest.ContentType = "application/json";
            httpRequest.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
            httpRequest.Headers.Add("HTTP-Referer", config.Referer);
            httpRequest.Headers.Add("X-Title", $"{config.Title} - Test");
            httpRequest.Timeout = config.TimeoutSeconds * 1000; // Convert to milliseconds

            using (var stream = httpRequest.GetRequestStream())
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            try
            {
                using (var httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string responseText = reader.ReadToEnd();
                    
                    // Debug: Show raw response structure
                    if (_debugMode)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine($"    [DEBUG Raw Response (first 1000 chars):]");
                        Console.WriteLine(responseText.Substring(0, Math.Min(1000, responseText.Length)));
                        Console.ResetColor();
                    }
                    
                    onComplete?.Invoke(HttpTransportResult.Ok(responseText));
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        string errorBody = reader.ReadToEnd();
                        int statusCode = (int)errorResponse.StatusCode;
                        
                        // Try to parse error response for better error message
                        try
                        {
                            var errorObj = JsonConvert.DeserializeObject<ChatCompletionResponse>(errorBody);
                            if (errorObj?.Error != null)
                            {
                                onComplete?.Invoke(HttpTransportResult.Fail($"API Error: {errorObj.Error.Message}", statusCode));
                                return;
                            }
                        }
                        catch { }

                        onComplete?.Invoke(HttpTransportResult.Fail($"HTTP {statusCode}: {errorBody}", statusCode));
                    }
                }
                else
                {
                    onComplete?.Invoke(HttpTransportResult.Fail($"Connection failed: {ex.Message}", 0));
                }
            }
        }
    }
    
    /// <summary>
    /// Test HTTP client for OpenRouter API.
    /// Uses OpenRouterClientCore with synchronous transport for console testing.
    /// This allows testing the actual prompt/response logic without needing Unity runtime.
    /// </summary>
    public class TestOpenRouterClient
    {
        private readonly TestConfig _config;
        private readonly OpenRouterClientCore _core;
        
        // For synchronous result capture
        private string _lastNarration;
        private ChoiceEventResponse _lastChoiceResponse;
        private string _lastError;
        private bool _lastTestSuccess;

        public TestOpenRouterClient(TestConfig config)
        {
            _config = config;
            
            var transport = new SyncHttpTransport(config.DebugMode);
            var transportConfig = new HttpTransportConfig
            {
                ApiKey = config.ApiKey,
                ApiUrl = "https://openrouter.ai/api/v1/chat/completions",
                TimeoutSeconds = 120, // 2 minutes for longer responses
                Referer = "https://rimworld-ainarrator.local",
                Title = "Tales from the RimWorld"
            };
            
            _core = new OpenRouterClientCore(
                transport,
                transportConfig,
                msg => { if (config.DebugMode) Console.WriteLine($"    [INFO] {msg}"); },
                msg => Console.WriteLine($"    [WARN] {msg}"),
                msg => Console.WriteLine($"    [ERROR] {msg}")
            );
        }

        /// <summary>
        /// Request narration for an event.
        /// Uses the shared OpenRouterClientCore.
        /// </summary>
        public string RequestNarration(string systemPrompt, string userPrompt)
        {
            _lastNarration = null;
            _lastError = null;
            
            var request = _core.BuildNarrationRequest(
                _config.Model,
                _config.Temperature,
                systemPrompt,
                userPrompt,
                _config.MaxNarrationTokens
            );

            // Since SyncHttpTransport is synchronous, the callbacks execute immediately
            _core.RequestNarration(request,
                narration => _lastNarration = narration,
                error => _lastError = error
            );
            
            if (_lastError != null)
            {
                throw new Exception(_lastError);
            }
            
            return _lastNarration;
        }

        /// <summary>
        /// Request a choice event.
        /// Uses the shared OpenRouterClientCore.
        /// </summary>
        public ChoiceEventResponse RequestChoiceEvent(string systemPrompt, string userPrompt)
        {
            _lastChoiceResponse = new ChoiceEventResponse();
            _lastError = null;
            
            var request = _core.BuildChoiceRequest(
                _config.Model,
                _config.Temperature,
                systemPrompt,
                userPrompt,
                _config.MaxChoiceTokens
            );

            _core.RequestChoiceEvent(request,
                result =>
                {
                    _lastChoiceResponse.Success = result.Success;
                    _lastChoiceResponse.Events = result.Events;
                    _lastChoiceResponse.RawContent = result.RawContent;
                },
                error =>
                {
                    _lastChoiceResponse.Success = false;
                    _lastChoiceResponse.Error = error;
                }
            );
            
            return _lastChoiceResponse;
        }

        /// <summary>
        /// Test API connection.
        /// Uses the shared OpenRouterClientCore.
        /// </summary>
        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;
            _lastTestSuccess = false;
            _lastError = null;
            
            // Use higher token limit for reasoning models (they need tokens to "think")
            bool isReasoningModel = _config.Model.Contains("preview") || 
                                    _config.Model.Contains("o1") || 
                                    _config.Model.Contains("o3");
            int testTokens = isReasoningModel ? 500 : 10;
            
            var request = _core.BuildTestRequest(_config.Model, testTokens);

            _core.TestConnection(request,
                () => _lastTestSuccess = true,
                error => _lastError = error
            );
            
            if (_lastError != null)
            {
                errorMessage = _lastError;
                return false;
            }
            
            return _lastTestSuccess;
        }
        
        /// <summary>
        /// Log usage information from a response.
        /// Call this after ParseChoiceEvent to show token usage.
        /// </summary>
        public void LogUsage(UsageInfo usage)
        {
            if (usage != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    [Tokens: {usage.PromptTokens} prompt + {usage.CompletionTokens} completion = {usage.TotalTokens} total]");
                Console.ResetColor();
            }
        }
    }

    #region Test-specific types

    /// <summary>
    /// Test configuration - separate from ModSettings since we're outside RimWorld.
    /// </summary>
    public class TestConfig
    {
        public string ApiKey { get; set; }
        public string Model { get; set; } = "google/gemini-2.0-flash-001";
        public float Temperature { get; set; } = 0.8f;
        public int MaxNarrationTokens { get; set; } = 200;
        public int MaxChoiceTokens { get; set; } = 2000;
        public bool DebugMode { get; set; } = false;
    }

    /// <summary>
    /// Test response wrapper for choice events.
    /// </summary>
    public class ChoiceEventResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string RawContent { get; set; }
        // Uses actual ChoiceEvent from AINarrator.LLMRequest
        public List<ChoiceEvent> Events { get; set; } = new List<ChoiceEvent>();
    }

    #endregion
}
