using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace AINarrator
{
    /// <summary>
    /// Async HTTP client for OpenRouter API communication.
    /// Uses Unity's coroutine system to avoid blocking the game thread.
    /// </summary>
    public static class OpenRouterClient
    {
        private const string API_URL = "https://openrouter.ai/api/v1/chat/completions";
        private const int TIMEOUT_SECONDS = 15;
        
        /// <summary>
        /// Request a narrative response from the LLM.
        /// </summary>
        public static void RequestNarration(string systemPrompt, string userPrompt, 
            Action<string> onSuccess, Action<string> onError, int maxTokens = 200)
        {
            if (!AINarratorMod.Settings.IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            var request = new ChatCompletionRequest
            {
                Model = AINarratorMod.Settings.SelectedModel,
                Temperature = AINarratorMod.Settings.Temperature,
                MaxTokens = maxTokens,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", userPrompt)
                }
            };
            
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Current.Root.StartCoroutine(SendRequestCoroutine(request, onSuccess, onError));
            });
        }
        
        /// <summary>
        /// Request a choice event from the LLM.
        /// </summary>
        public static void RequestChoiceEvent(string systemPrompt, string userPrompt,
            Action<ChoiceEvent> onSuccess, Action<string> onError)
        {
            if (!AINarratorMod.Settings.IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            var request = new ChatCompletionRequest
            {
                Model = AINarratorMod.Settings.SelectedModel,
                Temperature = AINarratorMod.Settings.Temperature,
                MaxTokens = 500,  // Choices need more tokens for structured output
                Messages = new List<ChatMessage>
                {
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", userPrompt)
                }
            };
            
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Current.Root.StartCoroutine(SendChoiceRequestCoroutine(request, onSuccess, onError));
            });
        }
        
        /// <summary>
        /// Test the API connection with a minimal request.
        /// </summary>
        public static void TestConnection(Action onSuccess, Action<string> onError)
        {
            if (!AINarratorMod.Settings.IsConfigured())
            {
                onError?.Invoke("API key not configured");
                return;
            }
            
            var request = new ChatCompletionRequest
            {
                Model = AINarratorMod.Settings.SelectedModel,
                Temperature = 0.5f,
                MaxTokens = 10,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage("user", "Say 'connected' in one word.")
                }
            };
            
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Current.Root.StartCoroutine(SendTestCoroutine(request, onSuccess, onError));
            });
        }
        
        private static IEnumerator SendRequestCoroutine(ChatCompletionRequest request,
            Action<string> onSuccess, Action<string> onError)
        {
            string jsonBody = JsonConvert.SerializeObject(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            using (UnityWebRequest www = new UnityWebRequest(API_URL, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {AINarratorMod.Settings.ApiKey}");
                www.SetRequestHeader("HTTP-Referer", "https://rimworld-ainarrator.local");
                www.SetRequestHeader("X-Title", "RimWorld AI Narrator");
                www.timeout = TIMEOUT_SECONDS;
                
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.ConnectionError || 
                    www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Log.Warning($"[AI Narrator] API Error: {www.error}");
                    onError?.Invoke($"Connection error: {www.error}");
                    yield break;
                }
                
                try
                {
                    var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(www.downloadHandler.text);
                    
                    if (response.Error != null)
                    {
                        Log.Warning($"[AI Narrator] API Error: {response.Error.Message}");
                        onError?.Invoke($"API error: {response.Error.Message}");
                        yield break;
                    }
                    
                    if (response.Choices == null || response.Choices.Count == 0)
                    {
                        onError?.Invoke("No response from API");
                        yield break;
                    }
                    
                    string content = response.Choices[0].Message?.Content ?? "";
                    onSuccess?.Invoke(content.Trim());
                }
                catch (Exception ex)
                {
                    Log.Error($"[AI Narrator] Parse error: {ex.Message}");
                    onError?.Invoke($"Failed to parse response: {ex.Message}");
                }
            }
        }
        
        private static IEnumerator SendChoiceRequestCoroutine(ChatCompletionRequest request,
            Action<ChoiceEvent> onSuccess, Action<string> onError)
        {
            string jsonBody = JsonConvert.SerializeObject(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            using (UnityWebRequest www = new UnityWebRequest(API_URL, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {AINarratorMod.Settings.ApiKey}");
                www.SetRequestHeader("HTTP-Referer", "https://rimworld-ainarrator.local");
                www.SetRequestHeader("X-Title", "RimWorld AI Narrator");
                www.timeout = TIMEOUT_SECONDS;
                
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.ConnectionError || 
                    www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Log.Warning($"[AI Narrator] API Error: {www.error}");
                    onError?.Invoke($"Connection error: {www.error}");
                    yield break;
                }
                
                try
                {
                    var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(www.downloadHandler.text);
                    
                    if (response.Error != null)
                    {
                        onError?.Invoke($"API error: {response.Error.Message}");
                        yield break;
                    }
                    
                    if (response.Choices == null || response.Choices.Count == 0)
                    {
                        onError?.Invoke("No response from API");
                        yield break;
                    }
                    
                    string content = response.Choices[0].Message?.Content ?? "";
                    
                    // Try to parse as JSON choice event
                    var choiceEvent = ParseChoiceEvent(content);
                    if (choiceEvent != null)
                    {
                        onSuccess?.Invoke(choiceEvent);
                    }
                    else
                    {
                        onError?.Invoke("Failed to parse choice event response");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[AI Narrator] Parse error: {ex.Message}");
                    onError?.Invoke($"Failed to parse response: {ex.Message}");
                }
            }
        }
        
        private static IEnumerator SendTestCoroutine(ChatCompletionRequest request,
            Action onSuccess, Action<string> onError)
        {
            string jsonBody = JsonConvert.SerializeObject(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            using (UnityWebRequest www = new UnityWebRequest(API_URL, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {AINarratorMod.Settings.ApiKey}");
                www.SetRequestHeader("HTTP-Referer", "https://rimworld-ainarrator.local");
                www.SetRequestHeader("X-Title", "RimWorld AI Narrator");
                www.timeout = TIMEOUT_SECONDS;
                
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    onError?.Invoke($"Connection failed: {www.error}");
                    yield break;
                }
                
                if (www.result == UnityWebRequest.Result.ProtocolError)
                {
                    // Check for auth error
                    if (www.responseCode == 401)
                    {
                        onError?.Invoke("Invalid API key");
                    }
                    else if (www.responseCode == 429)
                    {
                        onError?.Invoke("Rate limited - try again later");
                    }
                    else
                    {
                        onError?.Invoke($"HTTP error {www.responseCode}");
                    }
                    yield break;
                }
                
                try
                {
                    var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(www.downloadHandler.text);
                    
                    if (response.Error != null)
                    {
                        onError?.Invoke(response.Error.Message);
                        yield break;
                    }
                    
                    onSuccess?.Invoke();
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Parse LLM response into a ChoiceEvent structure.
        /// Expects JSON format in the response.
        /// </summary>
        private static ChoiceEvent ParseChoiceEvent(string content)
        {
            try
            {
                // Try to extract JSON from the response
                int jsonStart = content.IndexOf('{');
                int jsonEnd = content.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    string json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    return JsonConvert.DeserializeObject<ChoiceEvent>(json);
                }
                
                // If no JSON found, try to parse structured text
                return ParseStructuredChoice(content);
            }
            catch (Exception ex)
            {
                Log.Warning($"[AI Narrator] Failed to parse choice event: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Fallback parser for non-JSON structured responses.
        /// </summary>
        private static ChoiceEvent ParseStructuredChoice(string content)
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
    }
}

