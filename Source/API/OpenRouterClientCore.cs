using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AINarrator
{
    /// <summary>
    /// Result of parsing a choice event response.
    /// </summary>
    public class ChoiceEventResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string RawContent { get; set; }
        public List<ChoiceEvent> Events { get; set; } = new List<ChoiceEvent>();
        
        /// <summary>
        /// Get a single randomly selected event from the parsed events.
        /// </summary>
        public ChoiceEvent GetRandomEvent(Func<int, int, int> randomRange)
        {
            if (Events == null || Events.Count == 0) return null;
            int index = randomRange(0, Events.Count);
            return Events[index];
        }
    }
    
    /// <summary>
    /// Core OpenRouter client logic shared between production and test code.
    /// This class handles request building, response parsing, and business logic.
    /// The actual HTTP transport is injected via IHttpTransport.
    /// </summary>
    public class OpenRouterClientCore
    {
        private readonly IHttpTransport _transport;
        private readonly HttpTransportConfig _config;
        private readonly Action<string> _logMessage;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logError;
        
        /// <summary>
        /// Create a new OpenRouterClientCore with the specified transport and configuration.
        /// </summary>
        /// <param name="transport">HTTP transport implementation</param>
        /// <param name="config">Transport configuration (API key, URL, etc.)</param>
        /// <param name="logMessage">Optional logging callback for info messages</param>
        /// <param name="logWarning">Optional logging callback for warnings</param>
        /// <param name="logError">Optional logging callback for errors</param>
        public OpenRouterClientCore(
            IHttpTransport transport, 
            HttpTransportConfig config,
            Action<string> logMessage = null,
            Action<string> logWarning = null,
            Action<string> logError = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logMessage = logMessage ?? (_ => { });
            _logWarning = logWarning ?? (_ => { });
            _logError = logError ?? (_ => { });
        }
        
        /// <summary>
        /// Check if the client is properly configured.
        /// </summary>
        public bool IsConfigured() => !string.IsNullOrEmpty(_config.ApiKey);
        
        /// <summary>
        /// Build a ChatCompletionRequest for narration.
        /// </summary>
        public ChatCompletionRequest BuildNarrationRequest(
            string model, float temperature, string systemPrompt, string userPrompt, int maxTokens = 200)
        {
            return new ChatCompletionRequest
            {
                Model = model,
                Temperature = temperature,
                MaxTokens = maxTokens,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", userPrompt)
                }
            };
        }
        
        /// <summary>
        /// Build a ChatCompletionRequest for choice events.
        /// </summary>
        public ChatCompletionRequest BuildChoiceRequest(
            string model, float temperature, string systemPrompt, string userPrompt, int maxTokens = 2000)
        {
            return new ChatCompletionRequest
            {
                Model = model,
                Temperature = temperature,
                MaxTokens = maxTokens,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", userPrompt)
                }
            };
        }
        
        /// <summary>
        /// Build a simple test request.
        /// </summary>
        public ChatCompletionRequest BuildTestRequest(string model, int maxTokens = 10)
        {
            return new ChatCompletionRequest
            {
                Model = model,
                Temperature = 0.5f,
                MaxTokens = maxTokens,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage("user", "Say 'connected' in one word.")
                }
            };
        }
        
        /// <summary>
        /// Send a request and get a narration response.
        /// </summary>
        public void RequestNarration(ChatCompletionRequest request, Action<string> onSuccess, Action<string> onError)
        {
            if (!IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            string jsonBody = JsonConvert.SerializeObject(request);
            
            _transport.PostJson(jsonBody, _config, result =>
            {
                if (!result.Success)
                {
                    _logWarning($"[AI Narrator] API Error: {result.Error}");
                    onError?.Invoke($"Connection error: {result.Error}");
                    return;
                }
                
                try
                {
                    var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(result.ResponseBody);
                    var parseResult = ParseNarrationResponse(response);
                    
                    if (parseResult.Success)
                    {
                        onSuccess?.Invoke(parseResult.Content);
                    }
                    else
                    {
                        onError?.Invoke(parseResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logError($"[AI Narrator] Parse error: {ex.Message}");
                    onError?.Invoke($"Failed to parse response: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Send a request and get a choice event response.
        /// </summary>
        public void RequestChoiceEvent(ChatCompletionRequest request, Action<ChoiceEventResult> onSuccess, Action<string> onError)
        {
            if (!IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            string jsonBody = JsonConvert.SerializeObject(request);
            
            _transport.PostJson(jsonBody, _config, result =>
            {
                if (!result.Success)
                {
                    _logWarning($"[AI Narrator] API Error: {result.Error}");
                    onError?.Invoke($"Connection error: {result.Error}");
                    return;
                }
                
                try
                {
                    var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(result.ResponseBody);
                    
                    if (response.Error != null)
                    {
                        onError?.Invoke($"API error: {response.Error.Message}");
                        return;
                    }
                    
                    if (response.Choices == null || response.Choices.Count == 0)
                    {
                        onError?.Invoke("No response from API");
                        return;
                    }
                    
                    string content = response.Choices[0].Message?.Content ?? "";
                    var choiceResult = ParseChoiceEvent(content);
                    
                    if (choiceResult.Success)
                    {
                        onSuccess?.Invoke(choiceResult);
                    }
                    else
                    {
                        onError?.Invoke(choiceResult.Error ?? "Failed to parse choice event response");
                    }
                }
                catch (Exception ex)
                {
                    _logError($"[AI Narrator] Parse error: {ex.Message}");
                    onError?.Invoke($"Failed to parse response: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Test the API connection.
        /// </summary>
        public void TestConnection(ChatCompletionRequest request, Action onSuccess, Action<string> onError)
        {
            if (!IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            string jsonBody = JsonConvert.SerializeObject(request);
            
            _transport.PostJson(jsonBody, _config, result =>
            {
                if (!result.Success)
                {
                    if (result.StatusCode == 401)
                    {
                        onError?.Invoke("Invalid API key");
                    }
                    else if (result.StatusCode == 429)
                    {
                        onError?.Invoke("Rate limited - try again later");
                    }
                    else
                    {
                        onError?.Invoke(result.Error);
                    }
                    return;
                }
                
                try
                {
                    var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(result.ResponseBody);
                    
                    if (response.Error != null)
                    {
                        onError?.Invoke(response.Error.Message);
                        return;
                    }
                    
                    onSuccess?.Invoke();
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}");
                }
            });
        }
        
        #region Response Parsing
        
        /// <summary>
        /// Result of parsing a narration response.
        /// </summary>
        public class NarrationParseResult
        {
            public bool Success { get; set; }
            public string Content { get; set; }
            public string Error { get; set; }
            public UsageInfo Usage { get; set; }
        }
        
        /// <summary>
        /// Parse a narration response.
        /// </summary>
        public NarrationParseResult ParseNarrationResponse(ChatCompletionResponse response)
        {
            if (response.Error != null)
            {
                _logWarning($"[AI Narrator] API Error: {response.Error.Message}");
                return new NarrationParseResult 
                { 
                    Success = false, 
                    Error = $"API error: {response.Error.Message}" 
                };
            }
            
            if (response.Choices == null || response.Choices.Count == 0)
            {
                return new NarrationParseResult 
                { 
                    Success = false, 
                    Error = "No response from API" 
                };
            }
            
            string content = response.Choices[0].Message?.Content ?? "";
            return new NarrationParseResult 
            { 
                Success = true, 
                Content = content.Trim(),
                Usage = response.Usage
            };
        }
        
        /// <summary>
        /// Parse LLM response into a ChoiceEventResult structure.
        /// Expects JSON format with multiple events.
        /// </summary>
        public ChoiceEventResult ParseChoiceEvent(string content)
        {
            var result = new ChoiceEventResult { RawContent = content };
            
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    result.Error = "Empty response content from API";
                    return result;
                }
                
                // Try to extract JSON from the response
                int jsonStart = content.IndexOf('{');
                int jsonEnd = content.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    string json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    
                    // First try to parse as MultipleChoiceEventsResponse (new format)
                    var multiResponse = TryParseMultipleEvents(json);
                    if (multiResponse != null && multiResponse.Events != null && multiResponse.Events.Count > 0)
                    {
                        // Validate events have required fields
                        var validEvents = multiResponse.Events.FindAll(e => 
                            e != null && 
                            !string.IsNullOrEmpty(e.NarrativeText) && 
                            e.Options != null && 
                            e.Options.Count > 0);
                        
                        if (validEvents.Count > 0)
                        {
                            result.Events = validEvents;
                            result.Success = true;
                            return result;
                        }
                    }
                    
                    // Fallback: try to parse as single ChoiceEvent (legacy format)
                    try
                    {
                        var singleEvent = JsonConvert.DeserializeObject<ChoiceEvent>(json);
                        if (singleEvent != null && !string.IsNullOrEmpty(singleEvent.NarrativeText))
                        {
                            result.Events = new List<ChoiceEvent> { singleEvent };
                            result.Success = true;
                            return result;
                        }
                    }
                    catch { }
                    
                    result.Error = "Failed to parse choice event JSON - Events array was null or empty, or NarrativeText was missing";
                }
                else
                {
                    // If no JSON found, try to parse structured text
                    var structuredEvent = ParseStructuredChoice(content);
                    if (structuredEvent != null)
                    {
                        result.Events = new List<ChoiceEvent> { structuredEvent };
                        result.Success = true;
                        return result;
                    }
                    
                    result.Error = $"No JSON object found in response (content length: {content.Length})";
                }
            }
            catch (Exception ex)
            {
                _logWarning($"[AI Narrator] Failed to parse choice event: {ex.Message}");
                result.Error = $"Parse error: {ex.Message}";
            }
            
            return result;
        }
        
        /// <summary>
        /// Try to parse JSON as MultipleChoiceEventsResponse.
        /// Returns null if parsing fails.
        /// </summary>
        private MultipleChoiceEventsResponse TryParseMultipleEvents(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<MultipleChoiceEventsResponse>(json);
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Fallback parser for non-JSON structured responses.
        /// </summary>
        private ChoiceEvent ParseStructuredChoice(string content)
        {
            // Simple fallback - create a basic choice event from text
            var choiceEvent = new ChoiceEvent
            {
                NarrativeText = content.Split('\n')[0],
                Options = new List<ChoiceOption>()
            };
            
            // This is a basic fallback; the LLM should ideally return JSON
            return choiceEvent.Options.Count > 0 ? choiceEvent : null;
        }
        
        #endregion
    }
}


